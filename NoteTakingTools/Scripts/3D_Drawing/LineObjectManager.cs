using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

// Class responsible for spawning line objects
// One line object = 1 consecutive drawing
// Private line objects are instantiated, while public line objects are spawned through server
// The pen asks for spawning the object
// After the spawning is finished, the object is sent directly to the pen who then communicates with it
public class LineObjectManager : NetworkBehaviour
{

    [SerializeField]
    private GameObject lineObjectPrivatePrefab;

    [SerializeField]
    private GameObject lineObjectPublicPrefab;

    private GameObject lineObjectPublic;

    private GameObject lineObjectPrivate;

    private DrawingPen pen;


    public void SetPen(GameObject obj)
    {
        pen = obj.GetComponent<DrawingPen>();
    }

    public void SpawnPrivateDrawing()
    {
        lineObjectPrivate = null;
        lineObjectPublic = null;

        lineObjectPrivate = Instantiate(lineObjectPrivatePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        pen.SetPrivateDrawing(lineObjectPrivate);
    }

    [Command(requiresAuthority = false)]
    public void Cmd_SpawnPublicDrawing(NetworkConnectionToClient sender = null)
    {
        Rpc_SetNullObjects();

        lineObjectPublic = Instantiate(lineObjectPublicPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        NetworkServer.Spawn(lineObjectPublic);

        // The spawned public line object is sent directly to the client who asked for it
        // Only this client can coomunicate with the object and send it points
        Target_setPublicObject(sender, lineObjectPublic);
    }

    [ClientRpc]
    public void Rpc_SetNullObjects()
    {
        lineObjectPrivate = null;
        lineObjectPublic = null;
    }

    // Only the owner of the object will be able to send it points
    // The reason for the authority not being set is, that we want the object to 
    // stay in the scene even if the creator is disconnected
    [TargetRpc]
    public void Target_setPublicObject(NetworkConnection target, GameObject go)
    {
        lineObjectPublic = go;
        pen.SetPublicDrawing(lineObjectPublic);
    }
}
