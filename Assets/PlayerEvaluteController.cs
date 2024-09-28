using UnityEngine;

public class PlayerEvaluteController : MonoBehaviour
{

    public AnimLite.DancePlayable.DanceSetPlayerFromJson player;

    public float timeRate;

    void Update()
    {
        if (Input.GetKey("."))
        {
            this.player.Graph.Value.Evaluate(Time.deltaTime * this.timeRate);
        }

        if (Input.GetKey(","))
        {
            this.player.Graph.Value.Evaluate(Time.deltaTime * -this.timeRate);
        }
    }
}
