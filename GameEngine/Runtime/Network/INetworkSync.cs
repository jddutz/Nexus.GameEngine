namespace Main.Data
{
    public interface INetworkSync
    {
        bool IsNetworkSyncEnabled { get; set; }
        string NetworkId { get; set; }
        string NetworkOwner { get; set; }
        NetworkSyncModeEnum SyncMode { get; set; }
        float SyncInterval { get; set; }
        NetworkPriorityEnum Priority { get; set; }
        int NetworkChannel { get; set; }
        NetworkReliabilityEnum Reliability { get; set; }
        bool OwnerOnlyWrite { get; set; }
        HashSet<string> SynchronizedProperties { get; set; }
        HashSet<string> ExcludedProperties { get; set; }
        ConflictResolutionStrategyEnum ConflictResolution { get; set; }
        float SyncDistance { get; set; }
        bool UseDeltaCompression { get; set; }
        bool InterpolateUpdates { get; set; }
        int InterpolationBufferSize { get; set; }
        bool IsSynchronizing { get; }
        bool IsAuthoritative { get; }
        bool IsLocallyOwned { get; }
        int StateVersion { get; }
        DateTime? LastSyncTime { get; }
        DateTime? LastUpdateTime { get; }
        float NetworkLatency { get; }
        long BytesSent { get; }
        long BytesReceived { get; }
        int UpdatesSent { get; }
        int UpdatesReceived { get; }
        IReadOnlyList<string> SyncTargets { get; }
        event EventHandler<NetworkSyncEventArgs> StateReceived;
        event EventHandler<NetworkSyncEventArgs> StateSent;
        event EventHandler<NetworkSyncErrorEventArgs> SyncError;
        event EventHandler<OwnershipChangedEventArgs> OwnershipChanged;
        event EventHandler<ConflictDetectedEventArgs> ConflictDetected;
        event EventHandler<NetworkConnectionEventArgs> NetworkConnected;
        event EventHandler<NetworkConnectionEventArgs> NetworkDisconnected;
        event EventHandler<ClientJoinedEventArgs> ClientJoined;
        event EventHandler<ClientLeftEventArgs> ClientLeft;
        void SyncState();
        Task SyncStateAsync();
        void SyncStateTo(IEnumerable<string> targetClients);
        Task SyncStateToAsync(IEnumerable<string> targetClients);
        void ForceSyncState();
        void ApplyNetworkUpdate(NetworkUpdate update);
        NetworkUpdate CreateNetworkUpdate();
        NetworkUpdate CreateDeltaUpdate();
        bool RequestOwnership();
        Task<bool> RequestOwnershipAsync();
        bool TransferOwnership(string newOwner);
        void ReleaseOwnership();
        bool CanModify();
        bool CanModify(string clientId);
        void AddSyncTarget(string clientId);
        void RemoveSyncTarget(string clientId);
        void ClearSyncTargets();
        bool IsWithinSyncDistance(string clientId);
        bool ConnectToNetwork();
        Task<bool> ConnectToNetworkAsync();
        void DisconnectFromNetwork();
        NetworkStatistics GetNetworkStatistics();
        void ResetNetworkStatistics();
        NetworkValidationResult ValidateNetworkState();
        object ResolveConflict(object localState, object remoteState);
        void BackupState();
        void RestoreState();
        void SynchronizeProperty(string propertyName);
        void StopSynchronizingProperty(string propertyName);
        void ExcludeProperty(string propertyName);
        void IncludeProperty(string propertyName);
        void SetPropertyConflictResolver(string propertyName, Func<object, object, object> resolver);
        void RemovePropertyConflictResolver(string propertyName);
        void PauseSync();
        void ResumeSync();
        bool IsSyncPaused { get; }
        object PredictNetworkState(float futureTime);
    }
}
