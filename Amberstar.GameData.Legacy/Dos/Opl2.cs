namespace Amberstar.GameData.Legacy.Dos;

/// <summary>
/// A simple emulation of the Yamaha YM3812 (OPL2) of the Ad Lib sound card with its 9 melodic channels.
/// It is controlled by register writes like the chip and generates 16 bit mono samples.
///
/// The phase of an operator advances by its frequency. The output of the modulator changes the phase of the
/// carrier (up to 4 cycles) or is added to it. The envelope is an attenuation in dB (0 to 96 dB) with the
/// rates of the chip: 96 dB need 39.28 seconds with the decay rate 1 and the time halves with each rate step.
/// </summary>
internal sealed class Opl2
{
	public const double ChipRate = 49716.0;
	const int SineSize = 1024;
	const double MaxAttenuation = 96.0;
	static readonly double[] Sine = [.. Enumerable.Range(0, SineSize).Select(i => Math.Sin(2 * Math.PI * i / SineSize))];
	static readonly double[] Multipliers = [0.5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 12, 12, 15, 15];
	// attenuation of the key scale level in steps of 0.75 dB for the 16 highest values of the frequency number (block 7)
	static readonly int[] KeyScaleLevels = [0, 24, 32, 37, 40, 43, 45, 47, 48, 50, 51, 52, 53, 54, 55, 56];
	static readonly double[] KeyScaleFactors = [0.0, 0.5, 0.25, 1.0]; // 0, 3, 1.5 and 6 dB per octave

	enum EnvelopeState { Off, Attack, Decay, Sustain, Release }

	sealed class Operator
	{
		public bool Tremolo;
		public bool Vibrato;
		public bool Sustained;
		public bool KeyScaleRate;
		public int Multiplier;
		public int KeyScaleLevel;
		public int TotalLevel;
		public int AttackRate;
		public int DecayRate;
		public int SustainLevel;
		public int ReleaseRate;
		public int Waveform;
		public double Phase;
		public double Envelope = MaxAttenuation;
		public EnvelopeState State = EnvelopeState.Off;
		public double LastOutput;
		public double PreviousOutput;
	}

	sealed class Channel
	{
		public readonly Operator Modulator = new();
		public readonly Operator Carrier = new();
		public int FrequencyNumber;
		public int Block;
		public bool KeyOn;
		public int Feedback;
		public bool Additive;
	}

	readonly Channel[] channels = [.. Enumerable.Range(0, 9).Select(_ => new Channel())];
	readonly double sampleRate;
	bool waveformSelect;
	bool deepTremolo;
	bool deepVibrato;
	double tremoloPhase;
	double vibratoPhase;

	public Opl2(int sampleRate)
	{
		this.sampleRate = sampleRate;
	}

	// the operators of the register offsets 0x00 to 0x15 (-1 is unused)
	static readonly int[] OperatorChannels = [0, 1, 2, 0, 1, 2, -1, -1, 3, 4, 5, 3, 4, 5, -1, -1, 6, 7, 8, 6, 7, 8];
	static readonly bool[] OperatorIsCarrier = [false, false, false, true, true, true, false, false, false, false, false, true, true, true, false, false, false, false, false, true, true, true];

	Operator? GetOperator(int offset)
	{
		if (offset < 0 || offset >= OperatorChannels.Length || OperatorChannels[offset] < 0)
			return null;

		var channel = channels[OperatorChannels[offset]];

		return OperatorIsCarrier[offset] ? channel.Carrier : channel.Modulator;
	}

	public void Write(int register, int value)
	{
		switch (register & 0xf0)
		{
			case 0x00:
				if (register == 0x01)
					waveformSelect = (value & 0x20) != 0;
				break;
			case 0x20:
			case 0x30:
				if (GetOperator(register - 0x20) is Operator op20)
				{
					op20.Tremolo = (value & 0x80) != 0;
					op20.Vibrato = (value & 0x40) != 0;
					op20.Sustained = (value & 0x20) != 0;
					op20.KeyScaleRate = (value & 0x10) != 0;
					op20.Multiplier = value & 0xf;
				}
				break;
			case 0x40:
			case 0x50:
				if (GetOperator(register - 0x40) is Operator op40)
				{
					op40.KeyScaleLevel = value >> 6;
					op40.TotalLevel = value & 0x3f;
				}
				break;
			case 0x60:
			case 0x70:
				if (GetOperator(register - 0x60) is Operator op60)
				{
					op60.AttackRate = value >> 4;
					op60.DecayRate = value & 0xf;
				}
				break;
			case 0x80:
			case 0x90:
				if (GetOperator(register - 0x80) is Operator op80)
				{
					op80.SustainLevel = value >> 4;
					op80.ReleaseRate = value & 0xf;
				}
				break;
			case 0xa0:
				if (register <= 0xa8)
					channels[register - 0xa0].FrequencyNumber = (channels[register - 0xa0].FrequencyNumber & 0x300) | value;
				break;
			case 0xb0:
				if (register <= 0xb8)
				{
					var channel = channels[register - 0xb0];
					channel.FrequencyNumber = (channel.FrequencyNumber & 0xff) | ((value & 3) << 8);
					channel.Block = (value >> 2) & 7;
					bool keyOn = (value & 0x20) != 0;

					if (keyOn && !channel.KeyOn)
					{
						StartAttack(channel.Modulator);
						StartAttack(channel.Carrier);
					}
					else if (!keyOn && channel.KeyOn)
					{
						Release(channel.Modulator);
						Release(channel.Carrier);
					}

					channel.KeyOn = keyOn;
				}
				else if (register == 0xbd)
				{
					deepTremolo = (value & 0x80) != 0;
					deepVibrato = (value & 0x40) != 0;
				}
				break;
			case 0xc0:
				if (register <= 0xc8)
				{
					channels[register - 0xc0].Feedback = (value >> 1) & 7;
					channels[register - 0xc0].Additive = (value & 1) != 0;
				}
				break;
			case 0xe0:
			case 0xf0:
				if (GetOperator(register - 0xe0) is Operator opE0)
					opE0.Waveform = value & 3;
				break;
		}
	}

	static void StartAttack(Operator op)
	{
		op.State = EnvelopeState.Attack;
		op.Phase = 0;
	}

	static void Release(Operator op)
	{
		if (op.State != EnvelopeState.Off)
			op.State = EnvelopeState.Release;
	}

	/// <summary>
	/// The rate with the key scaling (0 to 63, 4 steps per rate value).
	/// </summary>
	static int EffectiveRate(Operator op, int rate, Channel channel)
	{
		if (rate == 0)
			return 0;

		int keyCode = (channel.Block << 1) | (channel.FrequencyNumber >> 9);

		return Math.Min(63, rate * 4 + (op.KeyScaleRate ? keyCode : keyCode >> 2));
	}

	// the time in seconds for 96 dB
	static double DecayTime(int effectiveRate) => 39.28 * Math.Pow(2.0, -(effectiveRate - 4) / 4.0);

	static double AttackTime(int effectiveRate) => 2.826 * Math.Pow(2.0, -(effectiveRate - 4) / 4.0);

	void UpdateEnvelope(Operator op, Channel channel, double seconds)
	{
		switch (op.State)
		{
			case EnvelopeState.Attack:
			{
				int rate = EffectiveRate(op, op.AttackRate, channel);

				if (rate >= 60)
				{
					op.Envelope = 0;
				}
				else if (rate > 0)
				{
					// exponential like the chip: fast at the start
					double factor = Math.Exp(-Math.Log(MaxAttenuation * 10) * seconds / AttackTime(rate));
					op.Envelope = op.Envelope * factor - 0.001;
				}

				if (op.Envelope <= 0)
				{
					op.Envelope = 0;
					op.State = EnvelopeState.Decay;
				}
				break;
			}
			case EnvelopeState.Decay:
			{
				double sustainLevel = op.SustainLevel == 15 ? MaxAttenuation : op.SustainLevel * 3.0;
				Decay(op, EffectiveRate(op, op.DecayRate, channel), seconds);

				if (op.Envelope >= sustainLevel)
				{
					op.Envelope = sustainLevel;
					op.State = EnvelopeState.Sustain;
				}
				break;
			}
			case EnvelopeState.Sustain:
				// percussive sounds fade with the release rate while the key is pressed
				if (!op.Sustained)
					Decay(op, EffectiveRate(op, op.ReleaseRate, channel), seconds);
				break;
			case EnvelopeState.Release:
				Decay(op, EffectiveRate(op, op.ReleaseRate, channel), seconds);
				break;
		}

		if (op.Envelope >= MaxAttenuation)
		{
			op.Envelope = MaxAttenuation;

			if (op.State == EnvelopeState.Release || (op.State == EnvelopeState.Sustain && !op.Sustained))
				op.State = EnvelopeState.Off;
		}
	}

	static void Decay(Operator op, int rate, double seconds)
	{
		if (rate > 0)
			op.Envelope += MaxAttenuation * seconds / DecayTime(rate);
	}

	double Wave(int waveform, double phase)
	{
		phase -= Math.Floor(phase);
		int index = (int)(phase * SineSize) & (SineSize - 1);
		double value = Sine[index];

		if (!waveformSelect)
			return value;

		return waveform switch
		{
			1 => value < 0 ? 0 : value,
			2 => Math.Abs(value),
			3 => (index & (SineSize / 4)) != 0 ? 0 : Math.Abs(value),
			_ => value
		};
	}

	double Output(Operator op, Channel channel, double modulation, double frequency, double tremolo, double vibrato, double seconds)
	{
		UpdateEnvelope(op, channel, seconds);

		if (op.State == EnvelopeState.Off)
			return 0;

		double opFrequency = frequency * Multipliers[op.Multiplier] * (op.Vibrato ? vibrato : 1.0);
		op.Phase += opFrequency * seconds;
		op.Phase -= Math.Floor(op.Phase);

		double keyScale = Math.Max(0, KeyScaleLevels[channel.FrequencyNumber >> 6] - 8 * (7 - channel.Block)) * 0.75 * KeyScaleFactors[op.KeyScaleLevel];
		double attenuation = op.Envelope + op.TotalLevel * 0.75 + keyScale + (op.Tremolo ? tremolo : 0);

		if (attenuation >= MaxAttenuation)
			return 0;

		return Wave(op.Waveform, op.Phase + modulation) * Math.Pow(10.0, -attenuation / 20.0);
	}

	/// <summary>
	/// Fills the buffer with samples.
	/// </summary>
	public void Generate(Span<short> buffer, double volume = 0.18)
	{
		double seconds = 1.0 / sampleRate;

		for (int i = 0; i < buffer.Length; i++)
		{
			// tremolo with 3.7 Hz (1 or 4.8 dB) and vibrato with 6.1 Hz (7 or 14 cent)
			tremoloPhase = (tremoloPhase + 3.7 * seconds) % 1.0;
			vibratoPhase = (vibratoPhase + 6.1 * seconds) % 1.0;
			double tremolo = (deepTremolo ? 4.8 : 1.0) * (1 - Math.Cos(2 * Math.PI * tremoloPhase)) / 2;
			double vibrato = Math.Pow(2.0, (deepVibrato ? 14 : 7) * Math.Sin(2 * Math.PI * vibratoPhase) / 1200.0);
			double sample = 0;

			foreach (var channel in channels)
			{
				if (channel.Modulator.State == EnvelopeState.Off && channel.Carrier.State == EnvelopeState.Off)
					continue;

				double frequency = channel.FrequencyNumber * ChipRate / (1 << (20 - channel.Block));
				var modulator = channel.Modulator;
				// the feedback is the average of the last 2 outputs (pi/16 to 4 pi)
				double feedback = channel.Feedback == 0 ? 0 : (modulator.LastOutput + modulator.PreviousOutput) * 2.0 / (1 << (7 - channel.Feedback));
				double modulatorOutput = Output(modulator, channel, feedback, frequency, tremolo, vibrato, seconds);
				modulator.PreviousOutput = modulator.LastOutput;
				modulator.LastOutput = modulatorOutput;

				if (channel.Additive)
					sample += modulatorOutput + Output(channel.Carrier, channel, 0, frequency, tremolo, vibrato, seconds);
				else
					sample += Output(channel.Carrier, channel, modulatorOutput * 4.0, frequency, tremolo, vibrato, seconds);
			}

			buffer[i] = (short)Math.Clamp(sample * volume * short.MaxValue, short.MinValue, short.MaxValue);
		}
	}
}
