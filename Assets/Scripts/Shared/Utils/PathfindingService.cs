using UnityEngine;
using UnityEngine.AI;

namespace Shared.Utils
{
    public static class PathfindingService
    {
        public static Vector3[] CalculatePath(Vector3 start, Vector3 end)
        {
            NavMeshHit hit;
            Vector3 finalDest = end;
            
            // 1. If end is invalid, sample nearest valid point
            if (NavMesh.SamplePosition(end, out hit, 10.0f, NavMesh.AllAreas)) 
            {
               finalDest = hit.position;
            }

            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(start, finalDest, NavMesh.AllAreas, path))
            {
                // Verify status
                if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
                {
                    return path.corners;
                }
            }
            
            // Fallback: direct line if path invalid (or off-mesh)
            return new Vector3[] { start, finalDest };
        }
    }
}
