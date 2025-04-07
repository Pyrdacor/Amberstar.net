namespace Amberstar.GameData;

public interface ISong
{
    bool Paused { get; }

    void Play();
    void Stop(bool reset = true);
    void Reset();
    void Update(double elapsed, IMusicPlayer musicPlayer);
}
