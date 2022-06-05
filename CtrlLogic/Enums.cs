namespace DayZServerControllerUI.CtrlLogic
{
    public enum CliArgumentIndices : int
    {
        SteamCmdPath = 0,
        DiscordFilePath,
        ModlistPath,
        DayZServerExecPath,
        DayZGameExecPath,
        RestartPeriod,
        WorkshopFolder
    };

    public enum SteamCmdModeEnum
    {
        SteamCmdExe,
        SteamPowerShellWrapper,
        Disabled
    }
}
