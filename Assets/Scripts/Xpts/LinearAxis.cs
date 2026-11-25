using UnityEngine;

namespace Entrepot.Xpts
{
    /// <summary>
    /// Axe linéaire à profil trapézoïdal : accélère, roule à vitesse maximale, freine pour s'arrêter pile sur la cible.
    /// Sert pour la translation de la navette, la sortie de la coulisse et la montée des nacelles.
    /// Classe C# pure (pas de MonoBehaviour) : testable seule.
    /// </summary>
    public sealed class LinearAxis
    {
        public float Position { get; private set; }
        public float Velocity { get; private set; }
        public float Target { get; set; }
        public float MaxSpeed;
        public float Acceleration;

        private const float Epsilon = 1e-4f;

        public LinearAxis(float position, float maxSpeed, float acceleration)
        {
            Position = Target = position;
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
        }

        public bool AtTarget => Mathf.Abs(Target - Position) < Epsilon && Mathf.Abs(Velocity) < 1e-3f;

        /// <summary>Avance la simulation de <paramref name="dt"/> secondes.</summary>
        public void Step(float dt)
        {
            float distance = Target - Position;
            if (Mathf.Abs(distance) < Epsilon) { Position = Target; Velocity = 0f; return; }

            float direction = Mathf.Sign(distance);
            float brakingDistance = Velocity * Velocity / (2f * Acceleration);
            bool movingToward = Mathf.Sign(Velocity) == direction && Velocity != 0f;
            float desired = movingToward && Mathf.Abs(distance) <= brakingDistance ? 0f : direction * MaxSpeed;

            Velocity = Mathf.MoveTowards(Velocity, desired, Acceleration * dt);
            // Évite de rester bloqué à vitesse nulle juste avant la cible.
            if (Mathf.Abs(Velocity) < 0.02f) Velocity = direction * 0.02f;

            float step = Velocity * dt;
            if (Mathf.Sign(step) == direction && Mathf.Abs(step) >= Mathf.Abs(distance)) { Position = Target; Velocity = 0f; }
            else Position += step;
        }

        /// <summary>Téléporte l'axe (sans mouvement).</summary>
        public void Reset(float position) { Position = Target = position; Velocity = 0f; }
    }
}
