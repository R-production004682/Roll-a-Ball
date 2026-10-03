/// <summary>
/// 本体演出の再生と放出停止後の粒子の消滅待ちを区別する
/// </summary>
internal enum ParticleEffectPlaybackState
{
    Idle,
    Playing,
    WaitingForParticles
}
