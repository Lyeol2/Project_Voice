using UnityEngine;

namespace Runtime.Singleton
{
    [System.Serializable]
    public abstract class MonoSingleton : MonoBehaviour
    {
        public abstract void Init(bool donDestroy = false);
    }
    public abstract class MonoSingleton<T> : MonoSingleton where T : MonoSingleton<T>
    {
        private static object syncObject = new object();

        private static T m_instance;

        public static T Instance
        {
            get
            {
                if (m_instance == null)
                {

                    lock (syncObject)
                    {
                        m_instance = FindFirstObjectByType<T>();

                        if (m_instance == null)
                        {
                            GameObject obj = new GameObject();
                            obj.name = typeof(T).Name;
                            m_instance = obj.AddComponent<T>();
                        }
                    }
                }

                return m_instance;
            }
        }

        public override void Init(bool donDestroy = false)
        {
            T inst = Instance;

            if(donDestroy)
                DontDestroyOnLoad(Instance.gameObject);


        }

        // 이미 인스턴스가 존재하는데 Awake 됬을때.
        protected virtual void Awake()
        {
            DestroyIfHasInstance();
        }


        //  이미 인스턴스가 존재한다면 삭제
        protected bool DestroyIfHasInstance()
        {
            if (m_instance == null)
            {
                m_instance = this as T;
                return false;
            }
            else
            {

                Destroy(gameObject);
                return true;
            }
        }

        private void OnDestroy()
        {
            if (m_instance != this)
                return;

            m_instance = null;
        }

        public static bool HasInstance()
        {
            return m_instance ? true : false;
        }

        public static void DestroyInstance()
        {
            if (m_instance != null && m_instance.gameObject != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(m_instance.gameObject);
#else 
                Destroy(m_instance.gameObject);
#endif
            }
        }

    }
}