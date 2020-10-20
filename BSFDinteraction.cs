using UnityEngine;
using HutongGames.PlayMaker;

namespace BSFD
{
    public class BSFDinteraction : MonoBehaviour
    {
        public RaycastHit hitInfo;
        private Part raycastedPart;
        bool isGuiActive = false;

        public bool hasHit = false;
        public float rayDistance = 1.35f;
        public int layerMaskBolts;
        public int layerMaskParts;

        public static AudioSource audioBoltScrew;

        public static PlayMakerFSM ratchetFsm;
        public static FsmBool ratchetSwitch;
        public static FsmBool GUIAssemble;
        public static FsmBool GUIDisassemble;
        public static FsmBool GUIuse;
        public static FsmFloat gameToolID;
        public static Transform ItemPivot;

        private Bolt activeBolt;

        // Use this for initialization
        void Start()
        {
            layerMaskBolts = LayerMask.GetMask("Bolts");
            layerMaskParts = LayerMask.GetMask("Parts");

            hitInfo = new RaycastHit();
            audioBoltScrew = GameObject.Find("MasterAudio/CarBuilding/bolt_screw").GetComponent<AudioSource>();
            ratchetFsm = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/2Spanner/Pivot/Ratchet").GetComponent<PlayMakerFSM>();
            ratchetSwitch = ratchetFsm.FsmVariables.FindFsmBool("Switch");
            GUIAssemble = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIassemble");
            GUIDisassemble = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIdisassemble");
            ItemPivot = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/1Hand_Assemble/ItemPivot").transform;
            GUIuse = PlayMakerGlobals.Instance.Variables.GetFsmBool("GUIuse").Value;
            gameToolID = PlayMakerGlobals.Instance.Variables.GetFsmFloat("ToolWrenchSize");

            Material boltActiveMaterial = new Material(Shader.Find("GUI/Text Shader"));
            boltActiveMaterial.color = new Color(0, 1, 0);

            if (boltActiveMaterial) Bolt.activeMaterial = boltActiveMaterial;
        }

        void FixedUpdate()
        {
            // Detach Parts
            RaycastHit hit;          
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, rayDistance, layerMaskParts))
            {
                raycastedPart = hit.collider.GetComponent<Part>();
                if(raycastedPart)
                {
                    // Part can be detached.
                    if(raycastedPart.isFitted && raycastedPart.tightness == 0)
                    {
                        // Show GUI
                        GUIDisassemble.Value = true;
                        isGuiActive = true;

                        // Detach Part by Input
                        if (Input.GetMouseButtonDown(1))
                        {
                            raycastedPart.Detach();
                            GUIDisassemble.Value = false;
                        }
                    }
                } 
            }
            else
            {
                // Disable GUI, if we activated it.
                if (isGuiActive)
                {
                    GUIDisassemble.Value = false;
                    isGuiActive = false;
                }
            }

            // don't update raycast, if not in tool mode
            if (gameToolID.Value == 0)
            {
                // deactivate bolt.
                if (activeBolt)
                {
                    activeBolt.Exit();
                    activeBolt = null;
                }
                // exit from the loop.
                return;
            }

            // do bolt raycast
            if (Camera.main != null) hasHit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, rayDistance, layerMaskBolts);

            // set active bolt
            if (hasHit && !activeBolt)
            {
                activeBolt = hitInfo.collider.GetComponent<Bolt>();
            }
            // deactivate bolt
            else if (!hasHit && activeBolt)
            {
                activeBolt.Exit();
                activeBolt = null;
            }
        }

        void Update()
        {
            // Update Active Bolt
            if (activeBolt) activeBolt.UpdateBolt();
        }

        //public bool GetHit(Collider collider) => hasHit && hitInfo.collider == collider;
        //public bool GetHitAny(Collider[] colliders) => hasHit && colliders.Any(collider => collider == hitInfo.collider);
        //public bool GetHitAny(List<Collider> colliders) => hasHit && colliders.Any(collider => collider == hitInfo.collider);
    }
}