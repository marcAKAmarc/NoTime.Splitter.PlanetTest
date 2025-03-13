namespace NoTime.Splitter.Core.Internal
{
    //This is not thread safe, but that is okay.
    public sealed class Statics
    {
        private static Statics instance = null;

        private int SceneCounter;

        private Statics()
        {
            SceneCounter = 0;
        }

        public static Statics Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Statics();
                }
                return instance;
            }
        }

        internal static int GetSceneCounter()
        {
            Instance.SceneCounter += 1;
            return Instance.SceneCounter;
        }

        internal static void Reset()
        {
            Instance.SceneCounter = 0;
        }
    }
}
