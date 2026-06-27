using UnityEngine;

public class TeamTag : MonoBehaviour
{
    public int teamId;

    public bool IsSameTeam(TeamTag other)
    {
        return other != null && teamId == other.teamId;
    }
}
