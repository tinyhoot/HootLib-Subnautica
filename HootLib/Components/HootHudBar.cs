using HootLib.Objects;
using TMPro;
using UnityEngine;

namespace HootLib.Components
{
    /// <summary>
    /// A template for creating your own custom hud bar appearing alongside food, water, etc.
    /// </summary>
    public abstract class HootHudBar : MonoBehaviour
    {
        public float Value { get; protected set; }
        public float Capacity { get; protected set; }
        public bool ShowElement = true;
        
        protected Animation _animation;
        protected GameObject _background;
        protected uGUI_CircularBar _bar;
        protected uGUI_SceneHUD _hud;
        protected RectTransform _icon;
        protected TextMeshProUGUI _text;

        protected float _angle;
        protected Color _barColor = Color.green;
        protected float _barCompletionTime = 0.5f;  // How long it should take the bar to reach the target value.
        protected float _barSpeed;
        protected float _currentBarPos;
        
        protected float _currentIconRotation;
        protected float _iconRotationSpringCoef;
        protected float _iconRotationVelDamp;
        protected float _iconRotationVelocity;
        protected float _lastFixedUpdateTime;

        protected DangerPulse _dangerPulse;

        /// <summary>
        /// Create the hud bar object from components belonging to other hud bars.
        /// </summary>
        /// <param name="name">The name to give the object in the hierarchy.</param>
        /// <param name="angle">The counter-clockwise angle to position the custom bar around the oxygen bar at.</param>
        /// <param name="gameObject">The GameObject that will hold all the components and smaller objects.</param>
        /// <returns>The subclassing HootHudBar component.</returns>
        public static T Create<T>(string name, float angle, out GameObject gameObject) where T: HootHudBar
        {
            var hud = uGUI.main.hud;
            // Create a copy of the health bar to use as a baseline.
            gameObject = Instantiate<GameObject>(hud.barHealth, hud.transform.Find("Content/BarsPanel"), false);
            gameObject.name = name;
            T component = gameObject.AddComponent<T>();
            component._angle = angle;
            gameObject.SetActive(true);
            return component;
        }

        protected virtual void Awake()
        {
            Value = 1;  // Set to 1 because 0 will cause the text to not appear for some reason.
            Capacity = 100;
            _hud = uGUI.main.hud;

            var healthBar = gameObject.GetComponent<uGUI_HealthBar>();
            CopyRelevantParts(healthBar);
            DestroyImmediate(healthBar);
            SetPosition(transform, _angle);
            // Prevent the custom components from being rendered on top of all the vanilla bars.
            transform.SetAsFirstSibling();
            
            _dangerPulse = new DangerPulse(_animation);
            _lastFixedUpdateTime = PDA.time;
        }

        protected virtual void Update()
        {
            if (ShowElement != gameObject.activeSelf)
                gameObject.SetActive(ShowElement);
        }

        protected virtual void LateUpdate()
        {
            PDA pda = Player.main.GetPDA();
            bool showNumbers = (pda != null && pda.isInUse);
            
            // Update the pulsing halo animation around the hud bar.
            _dangerPulse?.Update(Value / Capacity, PDA.deltaTime);
            // Flip the icon if we just switched to/from PDA mode.
            FlipIcon(showNumbers);
            // Move the bar closer to the current value if isn't there already.
            UpdateBarPosition(Value, Capacity);
        }

        /// <summary>
        /// Add a background to the hud bar. Kept separate so as not to cause too many overlaps with other custom huds.
        /// </summary>
        /// <param name="backgroundObject">Should be one of HUD.backgroundBarsDouble or HUD.backgroundBarsQuad.</param>
        /// <param name="angle">The angle around the oxygen bar at which to position the background.</param>
        protected void AddBackground(GameObject backgroundObject, float angle)
        {
            // Should really do this *after* moving the whole object so we don't get weird offset errors.
            _background = Instantiate(backgroundObject, transform, true);
            _background.transform.RotateAround(_hud.barOxygen.transform.position, Vector3.right, angle);
            _background.transform.SetAsFirstSibling();
            _background.SetActive(true);
        }

        /// <summary>
        /// Copy all parts from other hud bars that are useful for the custom bar.
        /// </summary>
        private void CopyRelevantParts(uGUI_HealthBar healthBar)
        {
            _animation = gameObject.GetComponent<Animation>();

            _bar = healthBar.bar;
            _bar.color = _barColor;
            _bar.UpdateMaterialBorderColor();

            _iconRotationSpringCoef = healthBar.rotationSpringCoef;
            _iconRotationVelDamp = healthBar.rotationVelocityDamp;
            
            _icon = healthBar.icon;
            _text = healthBar.text;
        }
        
        /// <summary>
        /// Rotate the icon/text in the middle of the bar.
        /// </summary>
        protected void FlipIcon(bool showNumbers)
        {
            if (MathExtensions.CoinRotation(ref _currentIconRotation, showNumbers ? 180f : 0f, 
                    ref _lastFixedUpdateTime, PDA.time, 
                    ref _iconRotationVelocity, _iconRotationSpringCoef, _iconRotationVelDamp, -1f))
            {
                _icon.localRotation = Quaternion.Euler(0f, _currentIconRotation, 0f);
            }
        }

        /// <summary>
        /// To ensure everything works correctly, harmony patch this into <see cref="uGUI_SceneHUD.UpdateElements()"/>!
        /// A simple postfix is enough.
        /// </summary>
        /// <param name="hud">The main HUD element.</param>
        public void HarmonyUpdateHudElements(uGUI_SceneHUD hud)
        {
            gameObject.SetActive(hud._active && ShowElement);
        }

        /// <summary>
        /// Set the position of this component around the oxygen bar.
        /// </summary>
        /// <param name="customTransform">The transform of this entire GameObject.</param>
        /// <param name="angle">The counter-clockwise angle to position the transform around the oxygen bar at.</param>
        protected void SetPosition(Transform customTransform, float angle)
        {
            // Position the custom bar in orbit around the oxygen bar.
            customTransform.RotateAround(_hud.barOxygen.transform.position, Vector3.right, angle);
            // Reorient the bar's elements to be right side up.
            customTransform.Rotate(Vector3.left, angle, Space.World);
        }

        /// <summary>
        /// Makes the bar move to a different value over several frames.
        /// </summary>
        protected void UpdateBarPosition(float value, float capacity)
        {
            float percentage = Mathf.Clamp01(value / capacity);
            // Don't do any of this math if the bar is already where it should be.
            if (Mathf.Approximately(_currentBarPos, percentage))
                return;
            
            _currentBarPos = Mathf.SmoothDamp(_currentBarPos, percentage, ref _barSpeed, _barCompletionTime,
                float.PositiveInfinity, PDA.deltaTime);
            // Ensure the bar does not get caught in tiny rounding errors and finishes within reasonable time.
            if (Mathf.Abs(_currentBarPos - percentage) < 0.002)
                _currentBarPos = percentage;
            _bar.value = _currentBarPos;
            _text.text = IntStringCache.GetStringForInt(Mathf.CeilToInt(_currentBarPos * capacity));
        }
    }
}