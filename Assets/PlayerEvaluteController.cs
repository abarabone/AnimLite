using UnityEngine;

public class PlayerEvaluteController : MonoBehaviour
{

    public AnimLite.DancePlayable.DanceSetPlayerFromJson player;

    public float timeRate;

    void Update()
    {
        if (this.player is null) return;

        if (Input.GetKey("."))
        {
            this.player.Graph.Value.Evaluate(Time.deltaTime * this.timeRate);
        }

        if (Input.GetKey(","))
        {
            this.player.Graph.Value.Evaluate(Time.deltaTime * -this.timeRate);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(this.player.Graph.Value.IsPlaying())
            {
                this.player.Graph.Value.Stop();
            }
            else
            {
                this.player.Graph.Value.Play();
            }
        }
    }
}
