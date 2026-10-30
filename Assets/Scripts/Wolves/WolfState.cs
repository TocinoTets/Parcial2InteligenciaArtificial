namespace IA.Agents
{
    public enum WolfState //estados de la FSM del lobo
    {
        Patrol, //recorre sus waypoints, usa A* si no ve el siguiente

        Chase, //ve al cazador y lo persigue directo

        GoToLastKnown, //va por A* a donde vio al cazador por ultima vez (o donde le avisaron con el aullido)

        Search //busca alrededor de esa ultima posicion
    }
}
