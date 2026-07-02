using ModLoader;
using UnityEngine;
using System;
using GeneratedUI;
using shaders.Lib;
using System.Collections.Generic;
using SFS.IO;

namespace shaders
{
    public class Main : Mod
    {
        public static FolderPath modFolder;

        public override string ModNameID => "shaders";
        public override string DisplayName => "SFS Shaders";
        public override string Author => "Cratior";
        public override string ModVersion => "2.9.0";
        public override string Description => "Shader manager/runtime.";
        public override string MinimumGameVersionNecessary => "1.5.6";

        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>
        {
            {
                "https://github.com/uneven-coder/sfs-shader-manager/releases/latest/download/sfs-shaders.dll",
                new FolderPath(ModFolder).ExtendToFile("sfs-shaders.dll")
            }
        };

        public override void Early_Load()
        {
            modFolder = new FolderPath(ModFolder);

            Try.Run(() => Lib.Patches.ApplyAll())
                .Match(
                    () => Debug.Log("[shaders] Patches applied."),
                    e => Debug.LogError($"[shaders] Patch error: {e}")
                );
        }

        public override void Load()
        {
            Try.Run(() =>
            {
                Lib.ShaderPackManager.Initialize();
                GeneratedLayout.Init();
            })
            .Match(
                () => Debug.Log("[shaders] Loaded."),
                e => Debug.LogError($"[shaders] Load error: {e}")
            );
        }
    }
}

namespace shaders.Lib
{
    /// <summary>Runs an action and captures any exception instead of throwing.</summary>
    public readonly struct Try
    {
        readonly Exception error;
        public bool Ok => error == null;

        Try(Exception e) => error = e;

        public static Try Run(Action a)
        {
            try { a(); return new Try(null); }
            catch (Exception e) { return new Try(e); }
        }

        public void Match(Action ok, Action<Exception> fail)
        {
            if (Ok) ok?.Invoke();
            else fail?.Invoke(error);
        }
    }

    /// <summary>Runs a func/action and captures the result or exception instead of throwing.</summary>
    public class Try<T>
    {
        public T Value { get; }
        public Exception Exception { get; }
        public bool IsSuccess => Exception == null;

        private Try(T value, Exception exception)
        {
            Value = value;
            Exception = exception;
        }

        public static Try<T> Run(Func<T> func)
        {
            try { return new Try<T>(func(), null); }
            catch (Exception ex) { return new Try<T>(default, ex); }
        }

        public static Try<T> Run(Action action)
        {
            try
            {
                action();
                return new Try<T>(default, null);
            }
            catch (Exception ex)
            {
                return new Try<T>(default, ex);
            }
        }

        public Try<U> Bind<U>(Func<T, Try<U>> func)
        {
            return IsSuccess ? func(Value) : new Try<U>(default, Exception);
        }

        public Try<U> Map<U>(Func<T, U> func)
        {
            return IsSuccess
                ? Try<U>.Run(() => func(Value))
                : new Try<U>(default, Exception);
        }

        public void Match(Action<T> onSuccess, Action<Exception> onFailure)
        {
            if (IsSuccess) onSuccess(Value);
            else onFailure(Exception);
        }
    }
}
