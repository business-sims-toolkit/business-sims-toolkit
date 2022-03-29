using Licensor;

namespace GameManagement
{
    public class DevOpsNetworkProgressionGameFile : NetworkProgressionGameFile
    {
        internal DevOpsNetworkProgressionGameFile(string filename, string roundOneFilesDir, bool isNew,
                bool allowSave, bool allowWriteToDisk, IGameLicence licence)
            : base(filename, roundOneFilesDir, isNew, allowSave, allowWriteToDisk, licence)
        {
        }

        public override bool GameTypeUsesTransitions()
        {
            return false;
        }

        public override int RoundToPhase(int round, GameFile.GamePhase gamePhase)
        {
            return round - 1;
        }

        public override void PhaseToRound(int phase, out int round, out GameFile.GamePhase gamePhase)
        {
            gamePhase = GamePhase.OPERATIONS;
            round = phase + 1;
        }

        public override int OpsRoundToBePlayedInSalesGame()
        {
            return 2;
        }
    }
}