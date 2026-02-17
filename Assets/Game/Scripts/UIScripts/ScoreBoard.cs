using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;

public class ScoreBoard : MonoBehaviour
{
    public static ScoreBoard instance;
    public Text blueTeamText;
    public Text redTeamText;
    public int blueTeamScore=0;
    public int redTeamScore=0;
    private PhotonView view;
    void Awake()
    {
        view=GetComponent<PhotonView>();
        instance=this;
    }
    public void PlayerDied(int playerTeam)
    {
        if (playerTeam == 2)
        {
            blueTeamScore++;
        }
        if (playerTeam == 1)
        {
            redTeamScore++;
        }
        view.RPC("UpdateScores",RpcTarget.All,blueTeamScore,redTeamScore);
    }
    [PunRPC]
    void UpdateScores(int blueScore,int redScore)
    {
        blueTeamScore=blueScore;
        redTeamScore=redScore;
        blueTeamText.text=blueTeamScore.ToString();
        redTeamText.text=redTeamScore.ToString();
    }
}
