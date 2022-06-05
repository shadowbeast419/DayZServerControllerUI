using CredentialManagement;

namespace DayZServerControllerUI.CtrlLogic
{
    internal static class WindowsCredentials
    {
        public static void TryGetExistingCredentials(string credentialName, out Credential? credentials)
        {
            // Look for an existing password
            credentials = GetPassword(credentialName);
        }

        public static bool SaveCredentials(string username, string password, string credentialName, out Credential? credentials)
        {
            credentials = null;

            // Store a new password
            if (!SavePassword(credentialName, username, password))
                return false;

            // Try to retrieve it
            credentials = GetPassword(credentialName);

            if (credentials == null)
                return false;

            return true;
        }

        private static bool SavePassword(string credentialName, string username, string password)
        {
            using (var cred = new Credential())
            {
                cred.Username = username;
                cred.Password = password;
                cred.Target = credentialName;
                cred.Type = CredentialType.Generic;
                cred.PersistanceType = PersistanceType.LocalComputer;
                return cred.Save();
            }
        }

        private static Credential? GetPassword(string credentialName)
        {
            Credential? cred = new Credential();
            cred.Target = credentialName;

            if (!cred.Load())
            {
                return null;
            }

            return cred;
        }
    }
}
