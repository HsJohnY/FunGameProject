namespace FunGame.Combat
{
    /// <summary>
    /// 干扰体攻击节奏产生的离散动作，便于表现层和未来主机权威层复用。
    /// </summary>
    public enum InterferenceEnemyAction
    {
        None = 0,
        TelegraphStarted = 1,
        AttackCommitted = 2
    }
}
