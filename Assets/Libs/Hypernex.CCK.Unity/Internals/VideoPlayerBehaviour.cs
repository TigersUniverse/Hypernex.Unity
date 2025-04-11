namespace Hypernex.CCK.Unity.Internals
{
    /// <summary>
    /// Unity Behaviours for VideoPlayers
    /// </summary>
    public abstract class VideoPlayerBehaviour
    {
        public virtual void Awake(){}
        public virtual void Start(){}
        public virtual void OnEnable(){}
        public virtual void OnDisable(){}
        public virtual void FixedUpdate(){}
        public virtual void Update(){}
        public virtual void LateUpdate(){}
        public virtual void OnAudioFilterRead(float[] data, int channels){}
        public virtual void OnGUI(){}
    }
}