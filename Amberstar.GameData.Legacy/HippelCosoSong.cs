namespace Amberstar.GameData.Legacy;

internal class HippelCosoSong : ISong
{
    internal record SongInfo(int StartDivision, int EndDivision, int InitialSpeed);

    internal record Instrument(params Instrument.Command[] Commands)
    {
        public enum CommandType
        {
            SetAbsolutePitch,
            SetRelativePitch,
            Loop,
            Complete,
            SetSample,
            ResetVolume,
            ResetTimbreAdjust,
            EnableToneAndNoise,
            DisableToneEnableNoise,
            EnableToneDisableNoise,
            Portando,
            NextCommand,
            Delay,
            SetTimbre,
        }

        public record Command(CommandType Type, params int[] Params);

        private int currentCommandIndex = 0;
        private int tickCounter = 0;

        public void Reset()
        {
            currentCommandIndex = 0;
            tickCounter = 0;
        }

        public void ProcessNextCommand(HippelCosoSong player)
        {
            if (--tickCounter > 0)
                return;

            var command = Commands[currentCommandIndex];

            switch (command.Type)
            {
                case CommandType.SetAbsolutePitch:
                    player.SetPitch(command.Params[0]);
                    ++currentCommandIndex;
                    break;
                case CommandType.SetRelativePitch:
                    player.SetPitch(player.GetPitch() + command.Params[0]);
                    ++currentCommandIndex;
                    break;
                case CommandType.Loop:
                    currentCommandIndex = command.Params[0];
                    ProcessNextCommand(player);
                    break;
                case CommandType.Complete:
                    // Just do nothing and don't increase the index.
                    break;
                case CommandType.SetSample:
                    player.SetSample(command.Params[0]);
                    ++currentCommandIndex;
                    ProcessNextCommand(player);
                    break;
                case CommandType.ResetVolume:
                    player.ResetVolume();
                    ++currentCommandIndex;
                    ProcessNextCommand(player);
                    break;
                case CommandType.ResetTimbreAdjust:
                    player.ResetTimbreAdjust();
                    ++currentCommandIndex;
                    ProcessNextCommand(player);
                    break;
                case CommandType.EnableToneAndNoise:
                    player.channels[player.currentVoice].NoisePeriod = command.Params[0];
                    player.channels[player.currentVoice].Tone = true;
                    player.channels[player.currentVoice].Noise = true;
                    ++currentCommandIndex;
                    ProcessNextCommand(player);
                    break;
                case CommandType.DisableToneEnableNoise:
                    player.channels[player.currentVoice].Tone = false;
                    player.channels[player.currentVoice].Noise = true;
                    ++currentCommandIndex;
                    ProcessNextCommand(player);
                    break;
                case CommandType.EnableToneDisableNoise:
                    player.channels[player.currentVoice].Tone = true;
                    player.channels[player.currentVoice].Noise = false;
                    ++currentCommandIndex;
                    ProcessNextCommand(player);
                    break;
                case CommandType.Portando:
                    player.channels[player.currentVoice].Portando = true;
                    player.channels[player.currentVoice].PortandoSlope = unchecked((sbyte)command.Params[0]);
                    ProcessNextCommand(player);
                    break;
                case CommandType.NextCommand:
                    ProcessNextCommand(player);
                    break;
                case CommandType.Delay:
                    tickCounter = command.Params[0];
                    break;
                case CommandType.SetTimbre:
                    player.channels[player.currentVoice].SetTimbre(command.Params[0]);
                    ProcessNextCommand(player);
                    break;
            }
        }
    }

    internal record Vibrato(int Slope, int Depth, int Delay);

    internal record Timbre(int Speed, int Instrument, Vibrato Vibrato, VolumeEnvelop VolumeEnvelop);

    internal record VolumeEnvelop(int Speed, params VolumeEnvelop.Command[] Commands)
    {
        public enum CommandType
        {
            SetVolume,
            Hold,
            Sustain,
            Loop,
        }

        public record Command(CommandType Type, params int[] Params);

        private int currentCommandIndex = 0;
        private int tickCounter = 0;

        public void Reset()
        {
            currentCommandIndex = 0;
            tickCounter = 0;
        }

        public void ProcessNextCommand(HippelCosoSong player)
        {
            if (--tickCounter > 0)
                return;

            var command = Commands[currentCommandIndex];

            switch (command.Type)
            {
                case CommandType.SetVolume:
                    player.SetVolume(command.Params[0]);
                    ++currentCommandIndex;
                    tickCounter = Speed;
                    break;
                case CommandType.Sustain:
                    ++currentCommandIndex;
                    tickCounter = command.Params[0];
                    break;
                case CommandType.Loop:
                    currentCommandIndex = command.Params[0];
                    ProcessNextCommand(player);
                    break;
                case CommandType.Hold:
                    // Just do nothing and don't increase the index.
                    break;
            }
        }
    }

    internal record Pattern(params Pattern.Command[] Commands)
    {
        public enum CommandType
        {
            SetNote, // and optionally timbre and/or instrument
            EndPattern,
            SetSpeed,
            SetSpeedWithDelay,
        }

        public record Command(CommandType Type, params int[] Params);

        private int currentCommandIndex = 0;
        private int tickCounter = 0;
        private int speed = 4;

        public void Reset(int initialSpeed)
        {
            currentCommandIndex = 0;
            tickCounter = 0;
            speed = initialSpeed;
        }

        public void ResetSpeed(int speed)
        {
            this.speed = speed;
        }

        public void ProcessNextCommand(HippelCosoSong player)
        {
            if (--tickCounter > 0)
                return;

            var command = Commands[currentCommandIndex];

            switch (command.Type)
            {
                case CommandType.SetNote:
                    player.SetNote(command.Params[0]);
                    if (command.Params.Length > 1 && command.Params[1] != 0)
                    {
                        // Set timbre
                        player.SetTimbre(player.GetTimbre() + command.Params[1]);
                    }
                    if (command.Params.Length > 2 && command.Params[2] != -1)
                    {
                        // Set instrument
                        player.SetInstrument(command.Params[2]);
                    }
                    ++currentCommandIndex;
                    tickCounter = speed;
                    break;
                case CommandType.SetSpeed:
                    speed = command.Params[0];
                    ++currentCommandIndex;
                    ProcessNextCommand(player);
                    break;
                case CommandType.SetSpeedWithDelay:
                    speed = command.Params[0];
                    ++currentCommandIndex;
                    tickCounter = speed;
                    break;
                case CommandType.EndPattern:
                    player.NextDivision();
                    break;
            }
        }
    }

    internal record Division(Division.Channel[] Channels)
    {
        public record Channel(int PatternIndex, int Transpose, int TimbreIndex, int VolumeReduction, int SpeedFactor, int TimbreAdjust);
    }

    internal class NotePlayer
    {
        private record NoteInfo(double Time, int Note, int Volume);
        private record NoiseInfo(double Time, int Period);

        private readonly Queue<NoteInfo> notePeriods = [];
        private readonly Queue<NoiseInfo> noisePeriods = [];

        private int volume = 64;
        private int notePeriod = -1;
        private int noisePeriod = 1;
        private int playedVolume = 64;
        private int playedNotePeriod = NotePeriods[0];

        // This happens every tick as long as the channel is active.
        public void PlayNote(double time, int period, int volume)
        {
            if (notePeriod != -1 && period == notePeriod && volume == this.volume)
                return;

            this.notePeriod = period;
            this.volume = volume;
            notePeriods.Enqueue(new(time, period, volume));
        }

        public void ChangeNoise(double time, int period)
        {
            //period = NotePeriods[period];
            if (period == 0)
                return;

            period = (byte)~period;
            period &= 0x1f;

            if (this.noisePeriod == period)
                return;

            this.noisePeriod = period;
            noisePeriods.Enqueue(new(time, period));
        }

        public void SampleData(sbyte[] buffer, double time, Action<int> noisePeriodChanger, Func<byte> nextNoiseTick, bool useTone, bool useNoise)
        {
            const double timePerSample = 1000.0 / SampleRate;
            var noteFrequency = 3546894.6 / playedNotePeriod;
            var noteVolume = playedVolume / 64.0;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (notePeriods.Count != 0 && notePeriods.Peek().Time <= time)
                {
                    var noteInfo = notePeriods.Dequeue();
                    playedNotePeriod = noteInfo.Note;
                    playedVolume = noteInfo.Volume;

                    noteFrequency = 3546894.6 / playedNotePeriod;
                    noteVolume = playedVolume / 64.0;
                }

                if (noisePeriods.Count != 0 && noisePeriods.Peek().Time <= time)
                {
                    var noiseInfo = noisePeriods.Dequeue();
                    noisePeriodChanger(noiseInfo.Period);
                }

                int div = useTone ? 1 : 0;
                div += useNoise ? 1 : 0;

                if (div == 0)
                {
                    buffer[i] = 0;
                }
                else
                {
                    double tone = useTone ? Math.Sin(2 * Math.PI * noteFrequency * time / SampleRate) : 0.0;
                    double noise = useNoise ? (nextNoiseTick() != 0 ? 1.0 : -1.0) : 0.0;

                    double sample = ((tone + noise) / div) * noteVolume;

                    buffer[i] = (sbyte)(sample * 127); // Convert to signed byte
                }

                time += timePerSample;
            }
        }
    }

    private static readonly word[] NotePeriods =
    [
        0x0eee, 0x0e17, 0x0d4d, 0x0c8e, 0x0bd9, 0x0b2f, 0x0a8e, 0x09f7,
        0x0967, 0x08e0, 0x0861, 0x07e8, 0x0777, 0x070b, 0x06a6, 0x0647,
        0x05ec, 0x0597, 0x0547, 0x04fb, 0x04b3, 0x0470, 0x0430, 0x03f4,
        0x03bb, 0x0385, 0x0353, 0x0323, 0x02f6, 0x02cb, 0x02a3, 0x027d,
        0x0259, 0x0238, 0x0218, 0x01fa, 0x01dd, 0x01c2, 0x01a9, 0x0191,
        0x017b, 0x0165, 0x0151, 0x013e, 0x012c, 0x011c, 0x010c, 0x00fd,
        0x00ee, 0x00e1, 0x00d4, 0x00c8, 0x00bd, 0x00b2, 0x00a8, 0x009f,
        0x0096, 0x008e, 0x0086, 0x007e, 0x0077, 0x0070, 0x006a, 0x0064,
        0x005e, 0x0059, 0x0054, 0x004f, 0x004b, 0x0047, 0x0043, 0x003f,
        0x003b, 0x0038, 0x0035, 0x0032, 0x002f, 0x002c, 0x002a, 0x0027,
        0x0025, 0x0023, 0x0021, 0x001f, 0x001d, 0x001c, 0x001a, 0x0019,
        0x0017, 0x0016, 0x0015, 0x0013, 0x0012, 0x0011, 0x0010, 0x000f,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000
    ];

    // TODO: Normally it should be 20ms per tick, but 120 works much better for some reason...
    const int TickTime = 120; // ms
    const int SampleRate = 44100; // Hz
    const int BufferSize = SampleRate / 4; // 0.25 second of audio
    const double BufferTime = BufferSize * 1000.0 / SampleRate; // in ms
    double elapsedTime = 0.0;
    double totalTime = 0.0;
    double lastSampleTime = 0.0;
    bool paused = false;
    bool playing = false;
    readonly int voiceCount = 0;
    int currentVoice = 0;
    readonly Instrument[] instruments;
    readonly Timbre[] timbres;
    readonly Division[] divisions;    
    readonly Pattern[] patterns;
    readonly Channel[] channels;
    readonly SongInfo songInfo;
    readonly NotePlayer[] channelPlayers;
    readonly sbyte[][] channelPcmData;
    readonly short[] mixedData;
    readonly byte[] pcmData;
    readonly byte[] noiseData;
    readonly YmNoiseGenerator noiseGenerator = new(1);
    bool prebuffered = false;
    double endOfStreamTime = 0.0;

    private class Channel(HippelCosoSong player, NotePlayer notePlayer, int channelIndex)
    {
        private int currentDivisionIndex = -1;
        private int currentInstrumentIndex = -1;
        private int currentTimbreIndex = -1;
        private Division.Channel? currentDivision;
        private Pattern? currentPattern;        
        private Instrument? currentInstrument;
        private Timbre? currentTimbre;

        public int Volume { get; set; }
        public int Pitch { get; set; }
        public int Note { get; set; }
        public int Sample { get; set; }
        public bool IsPlaying { get; set; }
        public int CurrentVibratoDelay { get; set; }
        public int CurrentVibratoDepth { get; set; }
        public int CurrentVibratoDirection { get; set; }
        public int PortandoSlope { get; set; }
        public int CurrentPortandoDelta { get; set; }
        public bool Portando { get; set; }
        public bool Noise { get; set; }
        public bool Tone { get; set; }
        public int NoisePeriod { get; set; }
        public int CurrentInstrument
        {
            get => currentInstrumentIndex;
            set
            {
                currentInstrumentIndex = value;
                currentInstrument = player.instruments[value];
            }
        }
        public int CurrentTimbre
        {
            get => currentTimbreIndex;
            set
            {
                currentTimbreIndex = value;
                currentTimbre = player.timbres[value];
            }
        }

        public void Reset()
        {
            Volume = 64;
            Pitch = 0;
            Note = 0;
            Sample = 0;
            CurrentVibratoDelay = 0;
            CurrentVibratoDepth = 0;
            CurrentVibratoDirection = -1;
            Portando = false;
            PortandoSlope = 0;
            CurrentPortandoDelta = 0;
            currentInstrumentIndex = -1;
            currentTimbreIndex = -1;
            currentInstrument = null;
            currentTimbre = null;
            currentDivision = null;
            currentPattern = null;
            IsPlaying = false;
        }

        public void ResetTimbreAdjust() => SetTimbre(currentDivision!.TimbreIndex);

        public void SetTimbre(int index)
        {
            if (CurrentTimbre == index)
                return;

            CurrentTimbre = index;
            CurrentInstrument = currentTimbre!.Instrument;

            int speed = player.songInfo.InitialSpeed;
            speed *= currentTimbre.Speed;
            speed *= currentDivision!.SpeedFactor;

            currentPattern!.ResetSpeed(speed);
            CurrentVibratoDelay = currentTimbre.Vibrato.Delay;
            CurrentVibratoDepth = currentTimbre.Vibrato.Depth;
            CurrentVibratoDirection = -1;
        }

        public void NextDivision()
        {
            currentDivisionIndex++;

            if (currentDivisionIndex >= player.songInfo.EndDivision)
            {
                currentDivisionIndex = player.songInfo.StartDivision;
                IsPlaying = false;
                return;
            }

            currentDivision = player.divisions[currentDivisionIndex].Channels[channelIndex];
            InitDivision();

            currentPattern!.ProcessNextCommand(player); // Directly process the next pattern in this case.
        }

        private void InitDivision()
        {
            CurrentTimbre = currentDivision!.TimbreIndex + currentDivision.TimbreAdjust;
            CurrentInstrument = currentTimbre!.Instrument;

            int speed = player.songInfo.InitialSpeed;
            speed *= currentTimbre.Speed;
            speed *= currentDivision.SpeedFactor;

            currentPattern = player.patterns[currentDivision!.PatternIndex];
            currentPattern!.Reset(speed);

            CurrentVibratoDelay = currentTimbre.Vibrato.Delay;
            CurrentVibratoDepth = currentTimbre.Vibrato.Depth;
            CurrentVibratoDirection = -1;
        }

        public void Update(double totalTime)
        {
            if (currentDivision == null) // start
            {
                IsPlaying = true;
                currentDivisionIndex = player.songInfo.StartDivision;
                currentDivision = player.divisions[currentDivisionIndex].Channels[channelIndex];
                InitDivision();
            }
            else if (!IsPlaying)
            {
                return;
            }

            bool wasNotUsingNoise = Noise;

            currentPattern!.ProcessNextCommand(player);
            currentTimbre!.VolumeEnvelop.ProcessNextCommand(player); // TODO: order?
            currentInstrument!.ProcessNextCommand(player);

            int note = Pitch;

            if ((note & 0x80) == 0)
                note += Note + currentDivision.Transpose;
            
            note &= 0x7f;

            int volume = Math.Max(0, Volume - currentDivision.VolumeReduction);
            int period = NotePeriods[note];

            // Vibrato
            if (CurrentVibratoDelay == 0)
            {
                CurrentVibratoDepth += CurrentVibratoDirection;

                if (CurrentVibratoDepth <= 0 || CurrentVibratoDepth >= 2 * currentTimbre.Vibrato.Depth)
                    CurrentVibratoDirection = -CurrentVibratoDirection;

                CurrentVibratoDepth = Math.Clamp(CurrentVibratoDepth, 0, 2 * currentTimbre.Vibrato.Depth);

                int diff = CurrentVibratoDepth - currentTimbre.Vibrato.Depth;

                period += (period * diff) / 1024;
            }
            else
            {
                --CurrentVibratoDelay;
            }

            // Portando
            if (Portando)
            {
                CurrentPortandoDelta += PortandoSlope;

                period *= (1 - (CurrentPortandoDelta * period) / 1024);
            }

            notePlayer.PlayNote(totalTime, period, volume);

            if (wasNotUsingNoise && Noise)
            {
                if (Tone) // If both (tone and noise) are active, e4 was used which set the NoisePeriod property.
                {
                    notePlayer.ChangeNoise(totalTime, NoisePeriod);
                }
                else if ((Pitch & 0x80) == 0) // Otherwise, e5 was used, so use the pitch logic.
                {
                    notePlayer.ChangeNoise(totalTime, (byte)~(Note + Pitch));
                }
                else
                {
                    notePlayer.ChangeNoise(totalTime, (byte)~(Pitch & 0x7f));
                }
            }
        }
    }

    internal HippelCosoSong(SongInfo songInfo, Instrument[] instruments, Timbre[] timbres, Division[] divisions, Pattern[] patterns)
    {
        voiceCount = divisions.Length == 0 ? 0 : divisions[0].Channels.Length;
        this.songInfo = songInfo;
        this.instruments = instruments;
        this.timbres = timbres;
        this.divisions = divisions;
        this.patterns = patterns;
        channels = new Channel[voiceCount];
        channelPlayers = new NotePlayer[voiceCount];
        channelPcmData = new sbyte[voiceCount][];
        mixedData = new short[BufferSize];
        pcmData = new byte[BufferSize];
        noiseData = new byte[BufferSize];

        for (int i = 0; i < voiceCount; ++i)
        {
            channelPcmData[i] = new sbyte[BufferSize]; // 0.25 second of audio
            var channelPlayer = channelPlayers[i] = new();
            var channel = channels[i] = new Channel(this, channelPlayer, i);
            channel.Reset();
        }

        PreBuffer();
    }

    public bool Paused
    {
        get => paused;
        set
        {
            if (paused == value)
                return;

            paused = value;
        }
    }

    public bool EndOfStream { get; private set; }

    private void PreBuffer()
    {
        bool wasPlaying = playing;
        playing = true;
        Update(BufferTime, null);
        playing = wasPlaying;
    }

    public void Play()
    {
        Stop();

        currentVoice = 0;
        playing = true;
    }
    
    public void Stop(bool reset = true)
    {
        if (!playing)
            return;

        playing = false;
        Paused = false;
        currentVoice = 0;

        if (reset)
            Reset();     
    }

    public void Reset()
    {
        elapsedTime = 0.0;
        totalTime = 0.0;
        EndOfStream = false;
        prebuffered = false;

        foreach (var channel in channels)
        {
            channel.Reset();
        }

        PreBuffer();
    }

    public void Update(double elapsed, IMusicPlayer? musicPlayer)
    {
        if (!playing || paused)
            return;

        if (prebuffered)
        {
            musicPlayer?.SampleData(pcmData, EndOfStream);
            prebuffered = musicPlayer == null;
        }

        double time = totalTime;
        elapsedTime += elapsed;
        totalTime += elapsed;

        int ticks = (int)Math.Floor(elapsedTime / TickTime);
        elapsedTime -= ticks * TickTime;

        while (ticks-- > 0)
        {
            currentVoice = 0;

            foreach (var channel in channels)
            {
                channel.Update(time);
                currentVoice++;
            }

            EndOfStream = channels.Any(channel => !channel.IsPlaying);

            if (EndOfStream)
            {
                endOfStreamTime = time;
                break;
            }

            time += TickTime;
        }

        if (totalTime - lastSampleTime >= BufferTime)
        {
            if (EndOfStream && endOfStreamTime == lastSampleTime)
            {
                musicPlayer?.SampleData([], true);
                return;
            }

            Array.Clear(mixedData);
            Array.Clear(noiseData);

            int noiseTickIndex = 0;

            byte GetNextNoiseTick()
            {
                if (noiseTickIndex < noiseData.Length)
                    return noiseData[noiseTickIndex++] = noiseGenerator.Tick(1);

                return noiseData[noiseTickIndex++ % noiseData.Length];
            }

            void ChangeNoisePeriod(int period)
            {
                noiseGenerator.Period = period;
            }

            for (int i = 0; i < voiceCount; i++)
            {
                channelPlayers[i].SampleData(channelPcmData[i], lastSampleTime,
                    ChangeNoisePeriod, GetNextNoiseTick, channels[i].Tone, channels[i].Noise);

                for (int b = 0; b < BufferSize; b++)
                {
                    mixedData[b] += channelPcmData[i][b];
                }
            }

            for (int b = 0; b < BufferSize; b++)
            {
                pcmData[b] = (byte)(128 + (mixedData[b] / voiceCount));
            }

            if (EndOfStream)
            {
                double duration = endOfStreamTime - lastSampleTime;
                int sampleCount = (int)(duration * SampleRate / 1000.0);

                if (sampleCount < pcmData.Length)
                {
                    musicPlayer?.SampleData(pcmData[..sampleCount], true);
                }
                else
                {
                    musicPlayer?.SampleData(pcmData, true);
                }

                elapsedTime = 0.0;
                totalTime = 0.0;
                lastSampleTime = 0.0;
                EndOfStream = false;

                foreach (var channel in channels)
                {
                    channel.Reset();
                }                
            }
            else
            {
                musicPlayer?.SampleData(pcmData, false);
                lastSampleTime += BufferTime;
            }

            prebuffered = pcmData == null;
        }
    }

    public void SetPitch(int pitch)
    {
        channels[currentVoice].Pitch = pitch;
    }

    public void SetNote(int note)
    {
        channels[currentVoice].Note = note;
    }

    public void SetVolume(int volume)
    {
        channels[currentVoice].Volume = volume;
    }

    public void ResetVolume()
    {
        channels[currentVoice].Volume = 64; // TODO: default?
    }

    public void ResetTimbreAdjust()
    {
        channels[currentVoice].ResetTimbreAdjust();
    }

    public void SetTimbre(int index)
    {
        channels[currentVoice].SetTimbre(index < timbres.Length ? index : 0);
    }

    public void SetInstrument(int index)
    {
        channels[currentVoice].CurrentInstrument = index < instruments.Length ? index : 0;
    }

    public void SetSample(int index)
    {
        channels[currentVoice].Sample = index;
    }

    public void NextDivision()
    {
        channels[currentVoice].NextDivision();
    }

    public int GetTimbre() => channels[currentVoice].CurrentTimbre;

    public int GetPitch() => channels[currentVoice].Pitch;

    public class YmNoiseGenerator
    {
        private uint lfsr = 0x1FFFF; // 17-bit LFSR initialized with all 1s
        private int period = 1; // Adjust based on YM noise period register (0-31)
        private byte currentOutput = 0;
        private double accumulator = 0;
        private double noiseClock;  // Hz

        public YmNoiseGenerator(int noisePeriod)
        {
            Period = noisePeriod;
        }

        public int Period
        {
            get => period;
            set
            {
                if (period == value)
                    return;

                period = Math.Clamp(value, 0, 31);
                noiseClock = 2_000_000.0 / (16.0 * (period + 1));
            }
        }

        // Simulate YM noise clock tick (should be called at the correct tick rate)
        public byte Tick(double sampleRate)
        {
            accumulator += noiseClock / sampleRate;

            if (accumulator >= 1.0)
            {
                // XOR taps: bit 0 and bit 3 (based on real YM behavior)
                bool bit0 = (lfsr & 1) != 0;
                bool bit3 = (lfsr & (1 << 3)) != 0;
                bool feedback = bit0 ^ bit3;

                // Shift right and insert feedback at bit 16
                lfsr >>= 1;

                if (feedback)
                    lfsr |= (1u << 16);

                accumulator -= 1.0;
                currentOutput = (byte)(lfsr & 1);
            }

            return currentOutput;
        }
    }

}
