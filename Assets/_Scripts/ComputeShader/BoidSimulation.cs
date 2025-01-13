using UnityEngine;

public class BoidSimulation : MonoBehaviour
{
    [System.Serializable]
    public struct BoidData
    {
        public Vector3 position;
        public Vector3 direction;
        public float speed;
    }

    public ComputeShader boidComputeShader;
    public int boidCount = 1000;
    public float neighborRadius = 5f;
    public float alignmentWeight = 1f;
    public float cohesionWeight = 1f;
    public float separationWeight = 1.5f;
    public float maxSpeed = 3f;
    public Mesh boidMesh;
    public Material boidMaterial;

    private ComputeBuffer boidBuffer;
    private BoidData[] boidArray;
    
    public Vector3 sphereCenter = Vector3.zero; 
    public float sphereRadius = 25f; 

    private void Start()
    {
        InitializeBoids();
        InitializeRendering();
    }

    private void Update()
    {
        RunComputeShader();
        RenderBoids();
    }

    private void InitializeBoids()
    {
        boidArray = new BoidData[boidCount];
        for (int i = 0; i < boidCount; i++)
        {
            boidArray[i].position = Random.insideUnitSphere * 10f;
            boidArray[i].direction = Random.insideUnitSphere.normalized;
            boidArray[i].speed = Random.Range(1f, maxSpeed);
        }

        boidBuffer = new ComputeBuffer(boidCount, sizeof(float) * 7); // 3 floats for position, 3 for direction, 1 for speed
        boidBuffer.SetData(boidArray);
    }
    
    private ComputeBuffer argsBuffer;

    private void InitializeRendering()
    {
        uint[] args = new uint[5] { boidMesh.GetIndexCount(0), (uint)boidCount, 0, 0, 0 };
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }


    private void RunComputeShader()
    {
        int kernel = boidComputeShader.FindKernel("CSMain");

        // Bind data and uniforms to the Compute Shader
        boidComputeShader.SetBuffer(kernel, "boidData", boidBuffer);
        boidComputeShader.SetFloat("neighborRadius", neighborRadius);
        boidComputeShader.SetFloat("separationWeight", separationWeight);
        boidComputeShader.SetFloat("alignmentWeight", alignmentWeight);
        boidComputeShader.SetFloat("cohesionWeight", cohesionWeight);
        boidComputeShader.SetFloat("maxSpeed", maxSpeed);
        boidComputeShader.SetFloat("deltaTime", Time.deltaTime);
        boidComputeShader.SetInt("bufferLength", boidCount);
        boidComputeShader.SetVector("sphereCenter", sphereCenter);
        boidComputeShader.SetFloat("sphereRadius", sphereRadius);


        // Dispatch the Compute Shader
        int threadGroups = Mathf.CeilToInt(boidCount / 256f);
        boidComputeShader.Dispatch(kernel, threadGroups, 1, 1);
    }

    private void RenderBoids()
    {
        boidMaterial.SetBuffer("boidData", boidBuffer);
        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, new Bounds(Vector3.zero, Vector3.one * 1000f), argsBuffer);
    }

    private void OnDestroy()
    {
        if (boidBuffer != null)
        {
            boidBuffer.Release();
        }
    }
}
