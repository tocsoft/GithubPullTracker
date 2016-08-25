using System;

namespace GithubPullTracker
{
    public interface IStringEncrypter : IDisposable
    {
        string Decrypt(string encrypted);
        string Encrypt(string unencrypted);
    }
}