using System.Collections.Generic;
using UnityEngine;


/// <summary>
///  Container for a particle
/// </summary>
public struct ParticlePSO
{
    public float[] position;
    public float[] bestPosition;
    public float[] speed;

}



public class PSO : MonoBehaviour {

   



    /// <summary>
    /// Simple function to optimize
    /// optimal solution for this is (0.5,0.5)
    /// </summary>
    /// <param name="data"> array of size 2 (x,y) </param>
    /// <returns></returns>
    public static float SimpleOptimFunction(float[] data)
    {
        float x = data[0];
        float y = data[1];

        if (x > 1 || x < 0 || y > 1 || y < 0)
        {
            return 0;
        }
        float n = 9; 
        float f1 = Mathf.Pow(15 * x * y * (1 - x) * (1 - y) * Mathf.Sin((float)n * Mathf.PI * x) * Mathf.Sin((float)n * Mathf.PI * y), 2);
        return -f1;
    }


    /// <summary>
    ///  Number of particles for the solver
    /// </summary>
    public int _NbParticles = 50;
    /// <summary>
    /// Time between two iterations of the algorithm (in seconds)
    /// </summary>
    public float _StepTime = 0.2f;

    /// <summary>
    /// Inertia parameter of pso
    /// </summary>
    public float _Inertia = 0.4f;

    /// <summary>
    ///  Local weight of pso 
    /// </summary>
    public float _LocalWeight = 1.0f;

    /// <summary>
    /// Global weight of pso
    /// </summary>
    public float _GlobalWeight = 1.0f;

    /// <summary>
    /// PSO algorithm object
    /// </summary>
    private PSO_Solver      m_solver;
    /// <summary>
    /// Visualization of particles
    /// </summary>
    private List<GameObject> m_particles;

    /// <summary>
    /// Time at the previous frame
    /// </summary>
    private float m_previousTime = 0;


    // Use this for initialization
    void Start () {

        m_solver = new PSO_Solver(_NbParticles, 2 , 0f, 1.0f, 0.4f, 1.0f, 1.0f);
        /// another function can be given to the solver (here is just an example)
        m_solver.SetOptimizationFunction(SimpleOptimFunction); // Look for the min value of this function
        m_solver.Init();

        /// Initialize visualization
        m_particles = new List<GameObject>(); 
        for (int i = 0; i < _NbParticles; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = Vector3.one * 0.05f;
            sphere.GetComponent<MeshRenderer>().material.color = Color.red;
            m_particles.Add(sphere); 
        }
        UpdateParticlePosition(); 

    }

    
    /// <summary>
    ///  Get the position from the solver and apply it to the game objects in the scene
    /// </summary>
    private void UpdateParticlePosition()
    {
        for(int i = 0; i < _NbParticles; i++)
        {
            ParticlePSO psop = m_solver._Particles[i];

            m_particles[i].transform.position = new Vector3(psop.position[0], (float)SimpleOptimFunction(psop.position), psop.position[1]); 
        }
    }

  
	// Update is called once per frame
	void Update ()
    {
        if ((Time.time-m_previousTime) > _StepTime)
        {
            m_previousTime = Time.time; 
            m_solver.Iteration();
            UpdateParticlePosition();
        }
       
	}
}

public class PSO_Solver
{
    
    // List of particles
    public List<ParticlePSO> _Particles;

    // Function to otpimize 
    public delegate float OptimizationFunction(float[] input);
    public OptimizationFunction toOptimize; 

    /// <summary>
    /// Number of particles
    /// </summary>
    private int m_swarmSize;
    /// <summary>
    /// Size of a particle (number of parameters)
    /// </summary>
    private int m_particleSize; 
    /// <summary>
    /// min value for particle paramter
    /// </summary>
    float m_minRange;
    /// <summary>
    /// max value for particle paramter
    /// </summary>
    float m_maxRange;
    /// <summary>
    /// Iniertia parameter
    /// </summary>
    private float m_inertia;
    /// <summary>
    /// local wheight  
    /// </summary>
    private float m_c1;
    /// <summary>
    /// Global wheight
    /// </summary>
    private float m_c2; // wheight global 
    /// <summary>
    /// Best recorded particle position
    /// </summary>
    public float [] bestWarmPosition;



    /// <summary>
    /// Constructor
    /// </summary>
    public PSO_Solver(int _sizeWarm, int _sizeParticle , float _lo , float _up, float _inertia, float _c1 , float _c2)
    {
        m_swarmSize = _sizeWarm;
        m_particleSize = _sizeParticle; 
        _Particles = new List<ParticlePSO>(); 
        m_minRange = _lo;
        m_maxRange = _up;
        m_inertia = _inertia;
        m_c1 = _c1;
        m_c2 = _c2; 
    }

    /// <summary>
    /// Set function to opitmize
    /// </summary>
    public void SetOptimizationFunction(OptimizationFunction func)
    {
        toOptimize = func; 
    }

    /// <summary>
    /// Initialize solver
    /// </summary>
    public void Init()
    {
        for (int i = 0; i < m_swarmSize; i++)
        {
            float[] pos = new float[m_particleSize];
            float[] speed = new float[m_particleSize];
            float normEcart = Mathf.Abs(m_maxRange - m_minRange);
            for (int j = 0;  j < m_particleSize; j++)
            {
                pos[j] = Random.Range(m_minRange, m_maxRange);
                speed[j] = Random.Range(-normEcart, normEcart);
            }
            ParticlePSO pso = new ParticlePSO();
            pso.position = pos;
            pso.speed = speed;
            pso.bestPosition = new float[m_particleSize];
            System.Array.Copy(pso.position, pso.bestPosition, pos.Length);
            if (i == 0)
            {
                bestWarmPosition = new float[m_particleSize];
                System.Array.Copy(pos, bestWarmPosition, m_particleSize); 
            }
            else
            {
                if (toOptimize(bestWarmPosition) > toOptimize(pos))
                {
                    System.Array.Copy(pos, bestWarmPosition, pos.Length);
                }
            }
            _Particles.Add(pso);
        }
    }



    /// <summary>
    /// Perform the minimization
    /// </summary>
    public void Iteration()
    {
        for (int i = 0; i < _Particles.Count; i++)
        {

            float r1 = Random.Range(0.0f, 1.0f);
            float r2 = Random.Range(0.0f, 1.0f);

            for(int j = 0; j  < _Particles[i].position.Length; j++)
            {
                _Particles[i].speed[j] = m_inertia * _Particles[i].speed[j] + m_c1 * r1 * (_Particles[i].bestPosition[j] - _Particles[i].position[j]) + m_c2 * r2 * (bestWarmPosition[j] - _Particles[i].position[j]); //  +φp rp(pi, d - xi, d) + φg rg(gd - xi, d)
                _Particles[i].position[j] = _Particles[i].position[j] + _Particles[i].speed[j];
                // constrain particle position in a limited range :
                _Particles[i].position[j] = Mathf.Max (m_minRange ,   Mathf.Min(m_maxRange, _Particles[i].position[j] )); 
            }

            if (toOptimize(_Particles[i].position) < toOptimize(_Particles[i].bestPosition))
            {
                /// Update particle best position
                System.Array.Copy(_Particles[i].position, _Particles[i].bestPosition, _Particles[i].position.Length);

                if (toOptimize(_Particles[i].position) < toOptimize(bestWarmPosition))
                {
                    /// Update global best position
                    System.Array.Copy(_Particles[i].position, bestWarmPosition, _Particles[i].position.Length);
                }
            }
        }
    }

    /// <summary>
    ///  Launch the complete algorithm
    /// </summary>
    public void Go(int nbIterations)
    {
        Init();
        for (int j = 0; j < nbIterations; j++)
        {
            Iteration();
        }
        Debug.Log(bestWarmPosition[0] + " " + bestWarmPosition[1]);
    }


}



