using System;
using Hypernex.Game.Avatar.FingerInterfacing;
using UnityEngine;
using UnityEngine.XR.OpenXR;

public class HandGetter : MonoBehaviour, IFingerCurler
{
    public bool EnableDebug;
    public Material mat;
    public Vector3[] positions;
    public Quaternion[] orientations;
    public Transform[] joints;
    public float[] radius;

    public HandTrackingFeature.Hand_Index HandIndex;

    public Hand Hand
    {
        get
        {
            switch (HandIndex)
            {
                case HandTrackingFeature.Hand_Index.L:
                    return Hand.Left;
                case HandTrackingFeature.Hand_Index.R:
                    return Hand.Right;
            }
            throw new Exception("Unknown HandIndex");
        }
    }
    public float ThumbCurl => Curls[0];
    public float IndexCurl => Curls[1];
    public float MiddleCurl => Curls[2];
    public float RingCurl => Curls[3];
    public float PinkyCurl => Curls[4];

#if DEBUG
    public GameObject DebugJointMesh;
    public GameObject[] DebugJointMeshes;
    public float DebugJointScale = 1f;
#endif

    public GameObject WristObject;

    private bool initializedOrientations;

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
    
    private struct BoneLineData
    {
        public LineRenderer r;
        public Vector3[] vertices;
        public int[] indices;
    }

    //line data for finger line renderers. indices are indices into OpenXR positions array
    private BoneLineData[] boneLines = new BoneLineData[]
    {
        new BoneLineData { vertices = new Vector3[2], indices = new int[] { 1, 0 } },   //palm
        new BoneLineData { vertices = new Vector3[5], indices = new int[] { 1, 2, 3, 4, 5 } },  //thumb
        new BoneLineData { vertices = new Vector3[6], indices = new int[] { 1, 6, 7, 8, 9, 10 } },  //index
        new BoneLineData { vertices = new Vector3[6], indices = new int[] { 1, 11, 12, 13, 14, 15 } },  //middle
        new BoneLineData { vertices = new Vector3[6], indices = new int[] { 1, 16, 17, 18, 19, 20 } },  //ring
        new BoneLineData { vertices = new Vector3[6], indices = new int[] { 1, 21, 22, 23, 24, 25 } }   //pinky
    };

    // Start is called before the first frame update
    void Start()
    {
        if (!EnableDebug)
            return;
        for(int i = 0; i < boneLines.Length; i++)
        {
            GameObject lineObj = new GameObject("Line");
            lineObj.transform.parent = transform;
            BoneLineData bld = boneLines[i];
            bld.r = lineObj.AddComponent<LineRenderer>();
            bld.r.startWidth = 0.01f;
            bld.r.endWidth = 0.01f;
            bld.r.sharedMaterial = mat;
            bld.r.positionCount = bld.vertices.Length;
            boneLines[i] = bld;
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandTrackingFeature hf=OpenXRSettings.Instance.GetFeature<HandTrackingFeature>();
        if(hf==null || hf.enabled==false)
        {
#if !UNITY_ANDROID
            print("You need to enable the openXR hand tracking support extension ");
#endif
        }
        
        if(hf)
        {
            hf.GetHandJoints(HandIndex, out positions, out orientations, out radius);
            if (positions.Length == 0) return;
            /*if (!initializedOrientations)
            {
                if (HandIndex == HandTrackingFeature.Hand_Index.L)
                {
                    FingerCalibration.InitialXRThumbs[0] = orientations[3];
                    FingerCalibration.InitialXRThumbs[1] = orientations[4];
                    FingerCalibration.InitialXRIndex[0] = orientations[7];
                    FingerCalibration.InitialXRIndex[1] = orientations[8];
                    FingerCalibration.InitialXRIndex[2] = orientations[9];
                    FingerCalibration.InitialXRMiddle[0] = orientations[12];
                    FingerCalibration.InitialXRMiddle[1] = orientations[13];
                    FingerCalibration.InitialXRMiddle[2] = orientations[14];
                    FingerCalibration.InitialXRRing[0] = orientations[17];
                    FingerCalibration.InitialXRRing[1] = orientations[18];
                    FingerCalibration.InitialXRRing[2] = orientations[19];
                    FingerCalibration.InitialXRLittle[0] = orientations[22];
                    FingerCalibration.InitialXRLittle[1] = orientations[23];
                    FingerCalibration.InitialXRLittle[2] = orientations[24];
                }
                else
                {
                    FingerCalibration.InitialXRThumbs[2] = orientations[3];
                    FingerCalibration.InitialXRThumbs[3] = orientations[4];
                    FingerCalibration.InitialXRIndex[3] = orientations[7];
                    FingerCalibration.InitialXRIndex[4] = orientations[8];
                    FingerCalibration.InitialXRIndex[5] = orientations[9];
                    FingerCalibration.InitialXRMiddle[3] = orientations[12];
                    FingerCalibration.InitialXRMiddle[4] = orientations[13];
                    FingerCalibration.InitialXRMiddle[5] = orientations[14];
                    FingerCalibration.InitialXRRing[3] = orientations[17];
                    FingerCalibration.InitialXRRing[4] = orientations[18];
                    FingerCalibration.InitialXRRing[5] = orientations[19];
                    FingerCalibration.InitialXRLittle[3] = orientations[22];
                    FingerCalibration.InitialXRLittle[4] = orientations[23];
                    FingerCalibration.InitialXRLittle[5] = orientations[24];
                }
                initializedOrientations = true;
            }*/
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
                            Transform obj = Instantiate(DebugJointMesh).transform;
                            obj.parent = joints[i];
                            obj.localPosition = Vector3.zero;
                            obj.localRotation = Quaternion.identity;
                            DebugJointMeshes[i] = obj.gameObject;
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

            if (!EnableDebug)
                return;
            //draw lines in game view
            for(int i = 0; i < boneLines.Length; i++)
            {
                BoneLineData bld = boneLines[i];
                for(int v = 0; v < bld.vertices.Length; v++) { bld.vertices[v] = positions[bld.indices[v]]; }
                bld.r.SetPositions(bld.vertices);
                boneLines[i] = bld;
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