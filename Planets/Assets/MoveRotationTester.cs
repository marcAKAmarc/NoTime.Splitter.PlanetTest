using NoTime.Splitter;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MoveRotationTester : MonoBehaviour
{
    public Rigidbody body;
    public Rigidbody sceneBody;
    public SplitterSubscriber splitterBody;
    public float rotationX;
    public float sensitivityX;

    Scene MainScene;
    Scene Scene;
    PhysicsScene PhysicsScene;

    public void Start()
    {
        MainScene = SceneManager.GetActiveScene();
        Scene = SceneManager.CreateScene(gameObject.name + System.Guid.NewGuid().ToString(), new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        PhysicsScene = Scene.GetPhysicsScene();

        SceneManager.SetActiveScene(Scene);
        sceneBody = (Instantiate(body.gameObject, Vector3.right * 5f, transform.localRotation)).GetComponent<Rigidbody>();
        SceneManager.SetActiveScene(MainScene);
    }
    public void Update()
    {
        rotationX += Input.GetAxis("Mouse X") * sensitivityX;
    }
    private void FixedUpdate()
    {
        Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
        Quaternion yQuaternion = Quaternion.AngleAxis(rotationX, Vector3.right);

        //this body doesn't rotate at all, because we first rotate one way, then rotate back.
        body.MoveRotation(body.rotation * xQuaternion);
        body.MoveRotation(body.rotation * Quaternion.Inverse(xQuaternion));

        //this body rotates... why?  it is as if the second call to MoveRotation overwrites the original
        Debug.Log("scene body before: " + sceneBody.rotation.eulerAngles.ToString("G6"));
        sceneBody.MoveRotation(xQuaternion * sceneBody.rotation);
        Debug.Log("scene body after 1: " + sceneBody.rotation.eulerAngles.ToString("G6"));
        sceneBody.MoveRotation(Quaternion.Inverse(xQuaternion) * sceneBody.rotation);
        Debug.Log("scene body after 2: " + sceneBody.rotation.eulerAngles.ToString("G6"));

        //this body rotates... why?  it is as if the second call to MoveRotation overwrites the original
        Debug.Log("splitter body before: " + splitterBody.AppliedPhysics.rotation.eulerAngles.ToString("G6"));
        splitterBody.AppliedPhysics.MoveRotation(xQuaternion * splitterBody.AppliedPhysics.rotation);
        Debug.Log("splitter body after 1: " + splitterBody.AppliedPhysics.rotation.eulerAngles.ToString("G6"));
        splitterBody.AppliedPhysics.MoveRotation(Quaternion.Inverse(yQuaternion) * splitterBody.AppliedPhysics.rotation);
        Debug.Log("splitter body after 2: " + splitterBody.AppliedPhysics.rotation.eulerAngles.ToString("G6"));

        /*body.MovePosition(body.position + (Vector3.forward * .005f));
        body.MovePosition(body.position + (Vector3.forward * -.005f));
        
        sceneBody.MovePosition(sceneBody.position + (Vector3.forward * .005f));
        sceneBody.MovePosition(sceneBody.position + (Vector3.forward * -.005f));*/

        PhysicsScene.Simulate(Time.fixedDeltaTime);
    }
}
