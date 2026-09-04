using System.Linq;
using FunGame.Combat;
using FunGame.Interaction;
using FunGame.Player;
using FunGame.UI;
using UnityEngine;

namespace FunGame.Demo
{
    public enum ExpeditionMode { Cooperative, Solo }

    /// <summary>唯一单人地图的启动适配器；在地图 Awake 前选择本地或主机权威逻辑。</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class SharedMapModeController : MonoBehaviour
    {
        public static ExpeditionMode NextMode { get; set; }
        [SerializeField] private GameObject mapRoot;
        [SerializeField] private GameObject networkRoot;
        [SerializeField] private GameObject menuCamera;
        [SerializeField] private FirstPersonController soloPlayer;
        [SerializeField] private Behaviour[] soloBehaviours;
        [SerializeField] private Behaviour[] networkBehaviours;
        [SerializeField] private GameObject replacementPipe;
        [SerializeField] private ContextInteractionProxy pumpProxy;
        [SerializeField] private MonoBehaviour soloPumpTarget;
        [SerializeField] private MonoBehaviour networkPumpTarget;
        public ExpeditionMode Mode { get; private set; }
        public GameObject MapRoot => mapRoot;
        public FirstPersonController SoloPlayer => soloPlayer;

        public void Configure(GameObject map, GameObject session, GameObject camera, FirstPersonController player,
            Behaviour[] local, Behaviour[] network, GameObject pipe, ContextInteractionProxy proxy,
            MonoBehaviour localPump, MonoBehaviour networkPump)
        {
            mapRoot = map; networkRoot = session; menuCamera = camera; soloPlayer = player;
            soloBehaviours = local; networkBehaviours = network; replacementPipe = pipe;
            pumpProxy = proxy; soloPumpTarget = localPump; networkPumpTarget = networkPump;
        }

        private void Awake()
        {
            Mode = NextMode;
            NextMode = ExpeditionMode.Cooperative;
            bool solo = Mode == ExpeditionMode.Solo;
            foreach (Behaviour behaviour in soloBehaviours) if (behaviour != null) behaviour.enabled = solo;
            foreach (Behaviour behaviour in networkBehaviours) if (behaviour != null) behaviour.enabled = !solo;
            soloPlayer.gameObject.SetActive(solo);
            replacementPipe.SetActive(solo);
            pumpProxy.Configure(solo ? soloPumpTarget : networkPumpTarget);
            pumpProxy.enabled = true;
            var campaign = mapRoot.GetComponentInChildren<SinglePlayerDemoController>(true);
            foreach (DemoEasterEgg325Interactable plate in mapRoot.GetComponentsInChildren<DemoEasterEgg325Interactable>(true))
            {
                plate.Configure(solo ? campaign : null);
                plate.enabled = true;
            }
            mapRoot.GetComponentInChildren<DemoChapterPresentation>(true).ConfigureNetworkMode(!solo);
            if (!solo)
                foreach (FunGame.Diagnostics.DevelopmentCheckpoint checkpoint in mapRoot.GetComponentsInChildren<FunGame.Diagnostics.DevelopmentCheckpoint>(true))
                    checkpoint.Configure("m4-coop-three-chapter-demo", "--m4-coop-smoke");
            GameMenuController menu = mapRoot.GetComponentInChildren<GameMenuController>(true);
            if (solo) menu.ConfigureForSinglePlayer(soloPlayer);
            else menu.ConfigureForNetworkSession();
            menuCamera.SetActive(!solo);
            mapRoot.SetActive(true);
            if (!solo)
                foreach (InterferenceEnemy enemy in mapRoot.GetComponentsInChildren<InterferenceEnemy>(true)) enemy.SetEncounterActive(false);
            networkRoot.SetActive(!solo);
        }
    }
}
