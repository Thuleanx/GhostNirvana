using UnityEngine;
using UnityEngine.InputSystem;
using NaughtyAttributes;
using Control;
using Utils;

namespace GhostNirvana {

public class MiyuController : MonoBehaviour, IPossessor<Miyu.Input> {
    const float inputBufferTime = 0.2f;

    public IDoll<Miyu.Input> _possessed;
    IDoll<Miyu.Input> IPossessor<Miyu.Input>.Possessed {
        get => _possessed;
        set => _possessed = value;
    }

    public Miyu.Input GetCommand() {
        return new Miyu.Input {
            desiredMovement = MovementToWorldDir(movement),
            targetPositionWS = MouseScreenToWorld(mousePosSS),
            shoot = isShooting || shootReleasedRecently
        };
    }

    [SerializeField] Miyu miyu;

    IPossessor<Miyu.Input> MiyuPossessor => this;

    [SerializeField, ReadOnly] Vector2 movement;
    [SerializeField, ReadOnly] Vector2 mousePosSS;
    bool isShooting;
    Timer shootReleasedRecently;

    public void OnMovement(InputAction.CallbackContext ctx) => movement = ctx.ReadValue<Vector2>();
    public void OnMousePos(InputAction.CallbackContext ctx) => mousePosSS = ctx.ReadValue<Vector2>();
    public void OnShoot(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            isShooting = true;
        } else if (ctx.canceled) {
            isShooting = false;
            shootReleasedRecently = inputBufferTime;
        }
    }

    void Awake() {
    }

    public void PossessMiyu() {
        MiyuPossessor.Possess(miyu);
    }

    Vector3 MovementToWorldDir(Vector2 movement) {
        Vector3 inputDir = new Vector3(
            movement.x, 0, movement.y
        ).normalized;

        return Quaternion.Euler(
            0, Camera.main.transform.eulerAngles.y, 0f
        ) * inputDir;
    }

    Vector3 MouseScreenToWorld(Vector2 mousePosScreenSpace) {
        Camera cam = Camera.main;
        Ray ray = Camera.main.ScreenPointToRay(mousePosSS);
        /* if (Physics.Raycast(ray, out RaycastHit hit)) */ 
        /*     return hit.point; */

        Plane plane = new Plane(Vector3.up, miyu ? miyu.BulletSource.position : transform.position);
        if (plane.Raycast(ray, out float dist)) {
            Vector3 pos = ray.GetPoint(dist);
            return pos;
        }
        return Camera.main.ScreenToWorldPoint(mousePosSS);
    }
}

}
