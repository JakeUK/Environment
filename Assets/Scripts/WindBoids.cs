using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WindBoids : MonoBehaviour
{

    #region Editor Visible Variables
    [Header("Output Settings")]
    public Vector2Int outputTextureSize;
    public RawImage outputImage;
    public bool clearTrail;

    [Header("Boid Settings")]
    [Min(1)]
    public int numberOfBoids = 1;
    public float speed = 1f;
    [Min(1)]
    public float detectionDistance = 1f;
    [Range(0, 360)]
    public float detectionAngle;

    [Header("Processing Settings")]
    [Range(0f, 1f)]
    public float fadeSpeed;
    #endregion

    #region Internal Variables
    private Boid[] boidHandler;
    #endregion

    #region Compute Shader Variables
    private RenderTexture outputBoidTexture;
    private ComputeShader boidCompute;
    private int[] computeDim;
    #endregion

    #region Compute Helpers
    private ComputeShader clearTextureCompute;
    #endregion

    struct Boid {
        public uint group;

        public Vector2 position;
        public float angle;

        public Vector4 color;

        public static int Size {
            get {
                return (sizeof(float) * 2) + (sizeof(float)) + (sizeof(float) * 4) + (sizeof(uint));
            }
        }
    }

    void OnEnable() {
        #region Load Resources
        boidCompute = Resources.Load<ComputeShader>("WindBoidsCompute");
        clearTextureCompute = Resources.Load<ComputeShader>("ClearTexture");
        #endregion
    }

    void Start() {
        outputBoidTexture = new RenderTexture(outputTextureSize.x, outputTextureSize.y, 0);
        outputBoidTexture.filterMode = FilterMode.Point;
        outputBoidTexture.enableRandomWrite = true;
        outputBoidTexture.Create();

        outputImage.texture = outputBoidTexture;

        //Perform initial setup
        SetUpBoids();
        computeDim = new int[2] { outputTextureSize.x, outputTextureSize.y };
    }

    //Set position, random direction, random colour
    void SetUpBoids() {
        boidHandler = new Boid[numberOfBoids];
        for (int i = 0; i < numberOfBoids; i++) {
            boidHandler[i] = new Boid {
                group = 0,
                position = new Vector2(Random.Range(0f, outputTextureSize.x), Random.Range(0f, 50f)),
                angle = 0f * Mathf.Deg2Rad,
                color = Random.ColorHSV()
            };
        }
    }

    void FixedUpdate() {
        //Clear agent map
        if (clearTrail) ClearTexture(outputBoidTexture);

        //Setup compute variables
        var boidBuffer = new ComputeBuffer(numberOfBoids, Boid.Size);
        boidBuffer.SetData(boidHandler);
        int numOfBatches = Mathf.CeilToInt(numberOfBoids / 8);

        //Set compute variables
        boidCompute.SetTexture(0, "_BoidMap", outputBoidTexture);
        boidCompute.SetBuffer(0, "_BoidData", boidBuffer);
        boidCompute.SetFloat("_BoidSpeed", speed);
        boidCompute.SetInts("_TextureDimensions", computeDim);
        boidCompute.SetInt("_BoidCount", numberOfBoids);
        boidCompute.SetFloat("_DetectionDistance", detectionDistance);
        boidCompute.SetFloat("_DetectionAngle", detectionAngle * Mathf.Deg2Rad);

        //Dispatch
        boidCompute.Dispatch(0, numOfBatches, 1, 1);

        //Read data and release buffer
        boidBuffer.GetData(boidHandler);
        boidBuffer.Release();
    }


    void ClearTexture(RenderTexture rt) {
        int numBatchX = Mathf.CeilToInt(rt.width / 8);
        int numBatchY = Mathf.CeilToInt(rt.height / 8);

        clearTextureCompute.SetTexture(0, "_Texture", rt);
        clearTextureCompute.SetFloat("width", rt.width);
        clearTextureCompute.SetFloat("height", rt.height);

        clearTextureCompute.Dispatch(0, numBatchX, numBatchY, 1);
    }

}
