using System;
using Process4;
using Ninject;
using NDesk.Options;
using Process4.Attributes;
using Protogame;

namespace ProtogameAssetManager
{
    [Distributed(Architecture.ServerClient, Caching.PushOnChange)]
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var connectToRunningGame = false;
            var options = new OptionSet
            {
                { "connect", "Internal use only (used by the game client).", v => connectToRunningGame = true }
            };
            try
            {
                options.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.Write("ProtogameAssetManager.exe: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `ProtogameAssetManager.exe --help` for more information.");
                return;
            }
            
            var kernel = new StandardKernel();
            kernel.Load<IoCModule>();
            kernel.Load<AssetIoCModule>();
            kernel.Load<PlatformingIoCModule>();
            kernel.Load<AssetManagerIoCModule>();

            if (connectToRunningGame)
            {
                var node = new LocalNode();
                node.Network = new ProtogameAssetManagerNetwork(node, true);
                node.Join();
                var assetManagerProvider = new NetworkedAssetManagerProvider(node, kernel);
                kernel.Bind<IAssetManagerProvider>().ToMethod(x => assetManagerProvider);
            }
            else
            {
                var assetManagerProvider = kernel.Get<LocalAssetManagerProvider>();
                kernel.Bind<IAssetManagerProvider>().ToMethod(x => assetManagerProvider);
            }

            using (var game = new AssetManagerGame(
                kernel,
                kernel.Get<IAssetManagerProvider>().GetAssetManager(true)))
            {
                game.Run();
            }
        }
    }
}
