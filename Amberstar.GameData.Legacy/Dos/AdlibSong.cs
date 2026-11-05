namespace Amberstar.GameData.Legacy.Dos;

/// <summary>
/// A song of the DOS version (Ad Lib MDI). It is a standard MIDI file (format 0, 420 ticks per quarter note) with
/// meta events of Ad Lib (FF 7F 00 00 3F) which give the instruments of the channels, the mode and the pitch bend range.
/// The MIDI channels 0 to 8 are the 9 melodic voices of the OPL2. The game plays it with the Ad Lib driver (ADLIB.C),
/// which is reproduced here: the instrument registers, the volume (velocity), the frequencies and the pitch bend.
/// The track length does not count the end of the track, and the song of the end (ENDE.MDI) is cut off,
/// so the end of the data also ends the song. The songs are repeated.
/// </summary>
internal sealed class AdlibSong : ISong
{
	const int SampleRate = 44100;
	const int BufferSize = SampleRate / 4;
	const double BufferTime = BufferSize * 1000.0 / SampleRate;
	const int VoiceCount = 9;
	const int DefaultTempo = 500000; // microseconds per quarter note
	// the register offsets of the modulators of the 9 voices (the carriers are 3 higher)
	static readonly int[] ModulatorOffsets = [0x00, 0x01, 0x02, 0x08, 0x09, 0x0a, 0x10, 0x11, 0x12];

	readonly byte[] module;
	readonly int trackStart;
	readonly int trackEnd;
	readonly int division;
	readonly short[] buffer = new short[BufferSize];
	readonly byte[][] timbres = new byte[VoiceCount][];
	readonly int[] volumes = new int[VoiceCount];
	readonly int[] notes = new int[VoiceCount];
	readonly int[] pitchBends = new int[VoiceCount];
	readonly bool[] keyOn = new bool[VoiceCount];
	Opl2 chip = new(SampleRate);
	int position;
	int runningStatus;
	int tempo;
	int pitchBendRange;
	double samplesUntilEvent;
	bool playing;
	double totalTime;
	double sentTime;

	public AdlibSong(byte[] module)
	{
		this.module = module;

		if (module.Length < 22 || module[0] != 'M' || module[1] != 'T' || module[2] != 'h' || module[3] != 'd')
			throw new InvalidDataException("The song is no MIDI file.");

		int headerLength = BigEndian(4, 4);
		division = BigEndian(12, 2);
		trackStart = 8 + headerLength + 8;
		trackEnd = module.Length;
		Reset();
	}

	int BigEndian(int offset, int size)
	{
		int value = 0;

		for (int i = 0; i < size; i++)
			value = (value << 8) | module[offset + i];

		return value;
	}

	public bool Paused { get; private set; }

	public void Play()
	{
		Reset();
		playing = true;
	}

	public void Stop(bool reset = true)
	{
		playing = false;

		if (reset)
			Reset();
	}

	public void Reset()
	{
		chip = new Opl2(SampleRate);
		chip.Write(0x01, 0x20); // waveform select
		chip.Write(0xbd, 0x00); // melodic mode
		position = trackStart;
		runningStatus = 0;
		tempo = DefaultTempo;
		pitchBendRange = 1;
		Array.Fill(volumes, 127);
		Array.Fill(pitchBends, 0x2000);
		Array.Clear(keyOn);
		Array.Clear(notes);
		Array.Clear(timbres);
		samplesUntilEvent = ReadDelta();
		totalTime = 0.0;
		sentTime = 0.0;
	}

	public void Update(double elapsed, IMusicPlayer musicPlayer)
	{
		if (!playing || Paused)
			return;

		totalTime += elapsed;

		// The samples are generated 1 buffer ahead.
		while (sentTime <= totalTime + BufferTime)
		{
			Render(buffer);
			musicPlayer.SampleData(buffer, false);
			sentTime += BufferTime;
		}
	}

	void Render(Span<short> output)
	{
		int offset = 0;

		while (offset < output.Length)
		{
			while (samplesUntilEvent < 1.0)
				samplesUntilEvent += ProcessEvent();

			int count = Math.Min(output.Length - offset, (int)samplesUntilEvent);
			chip.Generate(output.Slice(offset, count));
			offset += count;
			samplesUntilEvent -= count;
		}
	}

	double TicksToSamples(int ticks) => ticks * (tempo / (double)division) * SampleRate / 1_000_000.0;

	int ReadVariableLength()
	{
		int value = 0;

		while (position < trackEnd)
		{
			int b = module[position++];
			value = (value << 7) | (b & 0x7f);

			if ((b & 0x80) == 0)
				break;
		}

		return value;
	}

	double ReadDelta() => position < trackEnd ? TicksToSamples(ReadVariableLength()) : 0.0;

	int ReadByte() => position < trackEnd ? module[position++] : 0;

	/// <summary>
	/// Processes the next event and returns the samples until the following one.
	/// </summary>
	double ProcessEvent()
	{
		if (position >= trackEnd)
		{
			Restart();
			return ReadDelta() + 1.0;
		}

		int status = module[position];

		if (status < 0x80)
			status = runningStatus;
		else
			position++;

		if (status == 0xff)
		{
			int type = ReadByte();
			int length = ReadVariableLength();
			int start = position;
			position = Math.Min(trackEnd, position + length);

			switch (type)
			{
				case 0x2f: // end of track
					Restart();
					return ReadDelta() + 1.0;
				case 0x51 when length >= 3:
					tempo = (module[start] << 16) | (module[start + 1] << 8) | module[start + 2];
					break;
				case 0x7f when length >= 5 && module[start] == 0 && module[start + 1] == 0 && module[start + 2] == 0x3f:
					AdlibEvent(start + 3, length - 3);
					break;
			}
		}
		else if (status is 0xf0 or 0xf7)
		{
			position = Math.Min(trackEnd, position + ReadVariableLength());
		}
		else
		{
			runningStatus = status;
			int channel = status & 0xf;

			switch (status & 0xf0)
			{
				case 0x80:
					ReadByte();
					ReadByte();
					NoteOff(channel);
					break;
				case 0x90:
				{
					int note = ReadByte();
					int velocity = ReadByte();

					if (velocity == 0)
					{
						NoteOff(channel);
					}
					else
					{
						if (channel < VoiceCount && volumes[channel] != velocity)
						{
							volumes[channel] = velocity;
							UpdateVolume(channel);
						}

						NoteOn(channel, note);
					}
					break;
				}
				case 0xa0:
					ReadByte();
					SetVolume(channel, ReadByte());
					break;
				case 0xb0:
					ReadByte();
					ReadByte();
					break;
				case 0xc0:
					ReadByte();
					break;
				case 0xd0:
					SetVolume(channel, ReadByte());
					break;
				case 0xe0:
				{
					int low = ReadByte();
					int high = ReadByte();

					if (channel < VoiceCount)
					{
						pitchBends[channel] = (high << 7) | low;

						if (keyOn[channel])
							SetFrequency(channel, notes[channel], true);
					}
					break;
				}
			}
		}

		return ReadDelta();
	}

	void Restart()
	{
		for (int voice = 0; voice < VoiceCount; voice++)
			NoteOff(voice);

		position = trackStart;
		runningStatus = 0;
		tempo = DefaultTempo;
	}

	/// <summary>
	/// The meta events of Ad Lib: 1 = the instrument of a channel, 2 = the mode, 3 = the pitch bend range.
	/// </summary>
	void AdlibEvent(int offset, int length)
	{
		int type = (module[offset] << 8) | module[offset + 1];

		switch (type)
		{
			case 1 when length >= 3 + 28:
			{
				int channel = module[offset + 2];

				if (channel < VoiceCount)
				{
					timbres[channel] = module[(offset + 3)..(offset + 3 + 28)];
					SetTimbre(channel);
				}
				break;
			}
			case 3 when length >= 3:
				pitchBendRange = Math.Max(1, (int)module[offset + 2]);
				break;
		}
	}

	void SetVolume(int channel, int volume)
	{
		if (channel >= VoiceCount)
			return;

		volumes[channel] = volume;
		UpdateVolume(channel);
	}

	/// <summary>
	/// The instrument has 13 values of the modulator and the carrier (KSL, MULT, FB, AR, SL, EG, DR, RR, TL, AM,
	/// VIB, KSR, FM) and their waveforms.
	/// </summary>
	void SetTimbre(int voice)
	{
		var timbre = timbres[voice];

		for (int op = 0; op < 2; op++)
		{
			int slot = ModulatorOffsets[voice] + op * 3;
			int p = op * 13;
			chip.Write(0x20 + slot, (timbre[p + 9] != 0 ? 0x80 : 0) | (timbre[p + 10] != 0 ? 0x40 : 0)
				| (timbre[p + 5] != 0 ? 0x20 : 0) | (timbre[p + 11] != 0 ? 0x10 : 0) | (timbre[p + 1] & 0xf));
			chip.Write(0x60 + slot, ((timbre[p + 3] & 0xf) << 4) | (timbre[p + 6] & 0xf));
			chip.Write(0x80 + slot, ((timbre[p + 4] & 0xf) << 4) | (timbre[p + 7] & 0xf));
			chip.Write(0xe0 + slot, timbre[26 + op] & 3);
		}

		// the feedback and the connection are values of the modulator
		chip.Write(0xc0 + voice, ((timbre[2] & 7) << 1) | (timbre[12] != 0 ? 0 : 1));
		UpdateVolume(voice);
	}

	/// <summary>
	/// The volume changes the total level of the carrier, and of the modulator if both are added (like ADLIB.C).
	/// </summary>
	void UpdateVolume(int voice)
	{
		var timbre = timbres[voice];

		if (timbre == null)
			return;

		bool additive = timbre[12] == 0;

		for (int op = 0; op < 2; op++)
		{
			int p = op * 13;
			int level = timbre[p + 8] & 0x3f;

			if (op == 1 || additive)
			{
				int t = (63 - level) * volumes[voice];
				t += t + 127;
				level = 63 - t / 254;
			}

			chip.Write(0x40 + ModulatorOffsets[voice] + op * 3, ((timbre[p] & 3) << 6) | level);
		}
	}

	void NoteOn(int voice, int note)
	{
		if (voice >= VoiceCount)
			return;

		// a new note starts the sound again
		if (keyOn[voice])
			SetFrequency(voice, notes[voice], false);

		notes[voice] = note;
		keyOn[voice] = true;
		SetFrequency(voice, note, true);
	}

	void NoteOff(int voice)
	{
		if (voice >= VoiceCount)
			return;

		keyOn[voice] = false;
		SetFrequency(voice, notes[voice], false);
	}

	void SetFrequency(int voice, int note, bool key)
	{
		double bend = (pitchBends[voice] - 0x2000) / 8192.0 * pitchBendRange;
		double frequency = 440.0 * Math.Pow(2.0, (note - 69 + bend) / 12.0);
		int block = Math.Clamp((note - 12) / 12, 0, 7);
		int number = (int)Math.Round(frequency * (1 << (20 - block)) / Opl2.ChipRate);

		while (number > 1023 && block < 7)
		{
			block++;
			number = (int)Math.Round(frequency * (1 << (20 - block)) / Opl2.ChipRate);
		}

		number = Math.Clamp(number, 0, 1023);
		chip.Write(0xa0 + voice, number & 0xff);
		chip.Write(0xb0 + voice, (key ? 0x20 : 0) | (block << 2) | (number >> 8));
	}
}
