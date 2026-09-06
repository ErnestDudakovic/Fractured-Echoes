// Central place for the build indices of every scene in Fractured Echoes.
// The order here MUST match File > Build Profiles > Scene List.
//
// 0  GameCompany         - studio logo splash
// 1  Main Menu           - main menu
// 2  Opening Cut Scene   - intro cutscene, plays after "New Game"
// 3  HorrorScene         - the actual game
// 4  Game Victory        - end screen
public static class GameScenes
{
    public const int Splash   = 0;
    public const int MainMenu = 1;
    public const int CutScene = 2;
    public const int Game     = 3;
    public const int Victory  = 4;
}
