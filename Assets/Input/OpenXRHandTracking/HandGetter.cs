using UnityEngine;
using UnityEngine.XR.OpenXR;

public class HandGetter : MonoBehaviour
{
    // Start is called before the first frame update

    public Vector3[] positions;
    public Quaternion[] orientations;
    public Transform[] joints;
    public float[] radius;

    public HandTrackingFeature.Hand_Index HandIndex;

#if DEBUG
    public GameObject DebugJointMesh;
    public GameObject[] DebugJointMeshes;
    public float DebugJointScale = 1f;
#endif

    public GameObject WristObject;

    private float[] _localCurlsOpen = new float[5]
    {
        -83.6991f,
        3.24045038f,
        9.166323f,
        17.0771484f,
        12.6282425f
    };

    private float[] _localCurlsClosed = new float[5]
    {
        99.27916f,
        286.103638f,
        292.402283f,
        294.4036f,
        289.7828f
    };

    public float[] Curls = new float[5];

    // Update is called once per frame
    void Update()
    {
        HandTrackingFeature hf=OpenXRSettings.Instance.GetFeature<HandTrackingFeature>();
        if(hf==null || hf.enabled==false)
        {
            print("You need to enable the openXR hand tracking support extension ");
        }
        
        if(hf)
        {
            hf.GetHandJoints(HandIndex, out positions, out orientations, out radius);
            if (positions.Length == 0) return;
            if(joints == null || joints.Length == 0)
            {
                joints = new Transform[positions.Length];

                #if DEBUG
                if(DebugJointMesh != null)
                    DebugJointMeshes = new GameObject[positions.Length];
                #endif

                for(int i = 0; i < joints.Length;i++)
                {
                    joints[i] = new GameObject("Joint").transform;
                    joints[i].parent = transform;

                    #if DEBUG
                        if(DebugJointMeshes != null && DebugJointMeshes.Length > i)
                        {
                            GameObject obj = Instantiate(DebugJointMesh);
                            obj.transform.parent = joints[i];
                            obj.transform.localPosition = Vector3.zero;
                            obj.transform.localRotation = Quaternion.identity;
                            DebugJointMeshes[i] = obj;
                        }
                    #endif
                }
            }


            // Transform orientations back into SteamVR skeleton space
            if (HandIndex == HandTrackingFeature.Hand_Index.L)
                orientations[1] = orientations[1] * Quaternion.Euler(0, 0, -90); // Wrist is aligned to a different axis.. Don't ask me why
            else
                orientations[1] = orientations[1] * Quaternion.Euler(0, 0, 90);


            for (int i = 2; i < orientations.Length; i++)
            {
                orientations[i] = orientations[i] * Quaternion.Euler(0, -90, 0);
            }            
            
            for (int i = 0; i < positions.Length; i++)
            {
                joints[i].transform.position = positions[i];
                joints[i].transform.rotation = orientations[i];

                #if DEBUG
                if (DebugJointMeshes != null && DebugJointMeshes.Length > i)
                    DebugJointMeshes[i].transform.localScale = Vector3.one * radius[i] * DebugJointScale;
                #endif

                switch (i)
                {
                    case 1: //wrist
                        transform.position = joints[i].transform.position;
                        transform.rotation = joints[i].transform.rotation;
                        
                        if(WristObject != null)
                        {
                            WristObject.transform.position = transform.position;
                            WristObject.transform.rotation = transform.rotation;
                        }
                        break;
                    case 0: //palm
                        Debug.DrawLine(joints[1].transform.position, joints[i].transform.position, Color.red);
                        break;
                    case 2: case 6: case 11: case 16: case 21:  //metacarpals
                        Debug.DrawLine(joints[1].transform.position, joints[i].transform.position, Color.green);
                        break;
                    default:
                        Debug.DrawLine(joints[i - 1].transform.position, joints[i].transform.position, Color.blue);
                        break;
                }
            }

            float[] localCurls = new float[5];

            for(int i = 0; i < orientations.Length; i++)
            {
                if (i == 0)
                    continue;

                var localOrientation = Quaternion.Inverse(orientations[i-1]) *  orientations[i];

                switch (i)
                {
                    case 2: case 3: case 4: // thumb
                        localCurls[0] -= ConvertAngle(localOrientation.eulerAngles.z);
                        break;
                    case 7: case 8: case 9: // index
                        localCurls[1] -= ConvertAngle(localOrientation.eulerAngles.z);
                        break;
                    case 12: case 13: case 14: // middle
                        localCurls[2] -= ConvertAngle(localOrientation.eulerAngles.z);
                        break;
                    case 17: case 18: case 19: // ring
                        localCurls[3] -= ConvertAngle(localOrientation.eulerAngles.z);
                        break;
                    case 22: case 23: case 24: // pinky
                        localCurls[4] -= ConvertAngle(localOrientation.eulerAngles.z);
                        break;
                }
            }

            for(int i = 0; i < 5; i++)
            {
                Curls[i] = RemapClamped(localCurls[i], _localCurlsOpen[i], _localCurlsClosed[i]);
            }
        }
    }

    public static float RemapClamped(float value, float in1, float in2)
    {
        float t = (value - in1) / (in2 - in1);
        return Mathf.Clamp01(t);
    }

    public float ConvertAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}