using UnityEngine;
using System.Collections;

namespace BSFDTestbed
{
    public class Bolt : MonoBehaviour
    {
        public int currentBoltStep;
        public int maxBoltSteps = 8;
        public float boltSize;
        public float boltMoveAmount;

        public enum BoltingDirection { x, y, z }
        public BoltingDirection MoveAxis;
        public BoltingDirection RotateAxis;

        public static Material defaultMaterial;
        public static Material activeMaterial;

        Renderer renderer;
        bool isDelay = false;

        float x;
        float y;
        float z;

        float x1;
        float y1;
        float z1;

        // Use this for initialization
        void Start()
        {
            renderer = GetComponent<Renderer>();
            if (defaultMaterial == null) defaultMaterial = Instantiate(renderer.material) as Material;
        }

        void BoltTightenEvent(bool down, float delayTime)
        {
            if ((down && currentBoltStep > 0) || (!down && currentBoltStep < maxBoltSteps))
            {
                StartCoroutine(Delay(delayTime));
                MasterAudio.PlaySound3DAtTransform("CarBuilding", transform, 1f, 1f, 0f, "bolt_screw");
                currentBoltStep += down ? -1 : 1;

                if (RotateAxis == BoltingDirection.x)
                {
                    x = down ? -45 : 45;
                }
                else
                {
                    x = 0;
                }
                if (RotateAxis == BoltingDirection.y)
                {
                    y = down ? -45 : 45;
                }
                else
                {
                    y = 0;
                }
                if (RotateAxis == BoltingDirection.z)
                {
                    z = down ? -45 : 45;
                }
                else
                {
                    z = 0;
                }
                transform.localEulerAngles = transform.localEulerAngles += new Vector3(x, y, z);

                if (MoveAxis == BoltingDirection.x)
                {
                    x1 = down ? boltMoveAmount : -boltMoveAmount;
                }
                else
                {
                    x1 = 0;
                }
                if (MoveAxis == BoltingDirection.y)
                {
                    y1 = down ? boltMoveAmount : -boltMoveAmount;
                }
                else
                {
                    y1 = 0;
                }
                if (MoveAxis == BoltingDirection.z)
                {
                    z1 = down ? boltMoveAmount : -boltMoveAmount;
                }
                else
                {
                    z1 = 0;
                }
                transform.localPosition += new Vector3(x1, y1, z1);
            }
        }

        public void SetBoltStep(int newBoltStep)
        {
            if (newBoltStep < 0 || newBoltStep > maxBoltSteps)
            {
                MSCLoader.ModConsole.Print("BSFD: Tried set BoltStep to " + newBoltStep + ". BoltStep should be in range 0 - " + maxBoltSteps + ".");
                return;
            }

            int steps = 0;
            bool down = newBoltStep < currentBoltStep;
            steps = Mathf.Abs(currentBoltStep - newBoltStep);

            if (steps == 0) return; // we are already in target step -> quit.

            transform.localEulerAngles = transform.localEulerAngles + new Vector3(0, down ? -45 * steps : 45 * steps, 0);
            transform.localPosition += new Vector3(0, down ? boltMoveAmount * steps : -boltMoveAmount * steps, 0);
            currentBoltStep = newBoltStep;
        }

        // Update is called from Interaction.cs
        public void UpdateBolt()
        {
            if (boltSize == BSFDinteraction.gameToolID.Value)
            {
                // Set active material
                if (renderer.material != activeMaterial) SetActiveMaterial(true);

                if (Input.GetAxis("Mouse ScrollWheel") != 0 && !isDelay)
                {
                    // Rachet Logic
                    if (BSFDinteraction.ratchetFsm.Active) BoltTightenEvent(!BSFDinteraction.ratchetSwitch.Value, 0.08f);

                    // Spanner Logic                      
                    else BoltTightenEvent(Input.GetAxis("Mouse ScrollWheel") > 0 ? false : true, 0.28f);
                }
            }
            else
            {
                Exit();
            }
        }

        public void Exit()
        {
            if (renderer.material != defaultMaterial) SetActiveMaterial(false);
        }

        void SetActiveMaterial(bool active)
        {
            if (renderer && activeMaterial && defaultMaterial)
                renderer.material = active ? activeMaterial : defaultMaterial;
            else
                MSCLoader.ModConsole.Print("BSFD: Error when setting bolt material!");
        }

        IEnumerator Delay(float time)
        {
            isDelay = true;
            yield return new WaitForSeconds(time);
            isDelay = false;
        }
    }
}