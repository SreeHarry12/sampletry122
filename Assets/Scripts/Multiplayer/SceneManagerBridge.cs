using UnityEngine.SceneManagement;

namespace Rocket.Multiplayer
{
    public static class SceneManagerBridge
    {
        public static bool IsArenaScene()
        {
            return SceneManager.GetActiveScene().name == "Arena";
        }
    }
}
