using UnityEngine;
using Optimization;
using NaughtyAttributes;
using CombatSystem;
using Danmaku;
using Base;
using Utils;
using ScriptableBehaviour;
using UnityEngine.Events;
using System.Collections;

namespace GhostNirvana {

public partial class Miyu : PossessableAgent<Miyu.Input>, IHurtable, IHurtResponder {
    public static Miyu Instance;

    public enum States {
        Grounded,
        Dash,
        Dead
    }


    #region Components
    public MiyuStateMachine StateMachine { get; private set; }
    #endregion

    #region Progression
    [HorizontalLine(color:EColor.Blue)]
    [BoxGroup("Progression"), SerializeField, Expandable] LinearLimiterFloat xp;
    #endregion

    #region Movement
    [HorizontalLine(color:EColor.Blue)]
    [BoxGroup("Movement"), SerializeField, Expandable] LinearFloat movementSpeed;
    [BoxGroup("Movement"), Range(0, 64), SerializeField] float accelerationAlpha = 24;
    [BoxGroup("Movement"), Range(0, 64), SerializeField] float deccelerationAlpha = 12;
    [BoxGroup("Movement"), Range(0, 720), SerializeField] float turnSpeed = 24;
    #endregion

    #region Combat
    [HorizontalLine(color:EColor.Blue)]
    [BoxGroup("Combat"), SerializeField, Required]
    MovableAgentRuntimeSet allEnemies;
    [BoxGroup("Combat"), SerializeField, Required, ShowAssetPreview]
    Projectile projectilePrefab;
    [field:SerializeField, BoxGroup("Combat")] public Transform BulletSource {get; private set; }
    [BoxGroup("Combat"), SerializeField, Expandable] LinearLimiterFloat health;
    [BoxGroup("Combat"), SerializeField, Expandable] LinearFloat attackSpeed;
    [BoxGroup("Combat"), SerializeField, Expandable] LinearFloat bulletDamage;
    [BoxGroup("Combat"), SerializeField, Expandable] LinearFloat bulletSpeed;
    [BoxGroup("Combat"), SerializeField, Expandable] LinearFloat bulletKnockback;
    [BoxGroup("Combat"), SerializeField, Expandable] LinearLimiterFloat magazine;
    [BoxGroup("Combat"), SerializeField, Expandable] LinearFloat reloadRate;
    [BoxGroup("Combat"), SerializeField, Expandable] LinearFloat pushbackStrengthOnDamage;
    [BoxGroup("Combat"), SerializeField] float iframeSeconds;
    [BoxGroup("Combat"), SerializeField] UnityEvent<IHurtable, float, DamageType> _OnDamage;
    #endregion

    Timer iframeHappening;

    public Entity Owner => this;
    public bool IsDead => health.Value == 0;
    public bool HasBullet => magazine ? magazine.Value > 0 : false;
    public UnityEvent<IHurtable, float, DamageType> OnDamage => _OnDamage;

    protected override void Awake() {
		base.Awake();
        StateMachine = GetComponent<MiyuStateMachine>();
        Instance = this;

        health.Value = health.Limiter;
    }

    protected override void OnEnable() {
		base.OnEnable();
        IHurtResponder.ConnectChildrenHurtboxes(this);
    }

    protected void OnDisable() {
        IHurtResponder.DisconnectChildrenHurtboxes(this);
    }

    protected void Update() => PerformUpdate(StateMachine.RunUpdate);


    public void ShootProjectile(Vector3 targetDirection) {
        Projectile bullet = ObjectPoolManager.Instance.Borrow(gameObject.scene,
                projectilePrefab, BulletSource.position, BulletSource.rotation);

        bullet.Initialize(bulletDamage.Value, bulletKnockback.Value, targetDirection * bulletSpeed.Value);
    }

    void IHurtable.OnTakeDamage(float damageAmount, DamageType damageType, Hit hit) {
        bool killingHit = health.Value > 0 && health.Value <= damageAmount;
        health.Value -= damageAmount;
        health.CheckAndCorrectLimit();

        void PushAllEnemiesAway() {
            foreach (MovableAgent enemy in allEnemies) {
                Vector3 knockbackDir = enemy.transform.position - transform.position;
                knockbackDir.y = 0;
                knockbackDir.Normalize();

                (enemy as IKnockbackable).ApplyKnockback(pushbackStrengthOnDamage.Value, knockbackDir);
            }
        }
        PushAllEnemiesAway();

        iframeHappening = iframeSeconds;
        if (killingHit) OnDeath();
    }

    void OnDeath() {
        StateMachine.SetState(States.Dead);
    }

    public bool ValidateHit(Hit hit) => !IsDead && !iframeHappening;
    public void RespondToHurt(Hit hit) { }

    protected override IEnumerator IDispose() {
        // never actually disposes of the player
        while (true) yield return null;
    }
}

}
