// This file is part of Tmds.Ssh which is released under MIT.
// See file LICENSE for full license details.

using System.Buffers;
using System.Net.Security;
using System.Security.Principal;
#if NET8_0
using System.Runtime.CompilerServices;
#endif

namespace Tmds.Ssh;

// This wraps NegotiateAuthentication to provide an async API that accepts a CancellationToken.
sealed class AsyncNegotiateAuthentication : IDisposable
{
    private readonly NegotiateAuthentication _negotiateAuthentication;
    private int _state;
    private KrbLibSim.Auth _krbLibAuth;

    private enum State
    {
        Idle,
        InProgress,
        Disposed,
        DisposeRequested
    }

#if NET8_0
    // This API was made public in .NET 9 through ComputeIntegrityCheck.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetMIC")]
    private extern static void GetMICMethod(NegotiateAuthentication context, ReadOnlySpan<byte> data, IBufferWriter<byte> writer);
#endif

    public AsyncNegotiateAuthentication(NegotiateAuthenticationClientOptions clientOptions, string username)
    {
        if (KrbLibSim.ENABLED)
        {
            bool delegateCredential = clientOptions.AllowedImpersonationLevel == TokenImpersonationLevel.Delegation;
            _krbLibAuth = KrbLibSim.CreateAuth(username, clientOptions.TargetName, delegateCredential);
        }
        else
            _negotiateAuthentication = new NegotiateAuthentication(clientOptions);
    }

    public void Dispose()
    {
        if (KrbLibSim.ENABLED)
        {
            _krbLibAuth.Dispose();
        }
        else
        {
            while (true)
            {
                int state = Volatile.Read(ref _state);
                if (state == (int)State.Idle)
                {
                    // Try change from Idle to Disposed.
                    if (Interlocked.CompareExchange(ref _state, (int)State.Disposed, state) == state)
                    {
                        _negotiateAuthentication.Dispose();
                        return;
                    }
                }
                else if (state == (int)State.InProgress)
                {
                    // Try change from InProgress to DisposeRequested.
                    if (Interlocked.CompareExchange(ref _state, (int)State.DisposeRequested, state) == state)
                    {
                        return;
                    }
                }
                else
                {
                    // Disposed or DisposeRequested.
                    return;
                }
            }
        }
    }

    public bool IsSigned
    {
        get
        {
            return KrbLibSim.ENABLED ? _krbLibAuth.IsSigned : _negotiateAuthentication.IsSigned;
        }
    }

    public async Task<(byte[]? outgoingBlob, NegotiateAuthenticationStatusCode statusCode)> GetOutgoingBlobAsync(byte[] incomingBlob, CancellationToken cancellationToken)
    {
        /*
                try
                {
                    byte[]? outgoingBlob = _negotiateAuthentication.GetOutgoingBlob(incomingBlob, out NegotiateAuthenticationStatusCode statusCode);
                    return (outgoingBlob, statusCode);
                }
                finally
                {
                    State previous = (State)Interlocked.CompareExchange(ref _state, (int)State.Idle, (int)State.InProgress);
                    // When an async operation is cancelled, Dispose may have been called already
                    // and we're responsible for disposing the NegotiateAuthentication.
                    if (previous == State.DisposeRequested)
                    {
                        _negotiateAuthentication.Dispose();
                    }
                }

                Task<(byte[]? outgoingBlob, NegotiateAuthenticationStatusCode)> result = Task.Run(() =>
                { var barr = new byte[10]; return (barr, NegotiateAuthenticationStatusCode.GenericFailure); });
        */
        if (KrbLibSim.ENABLED)
        {
            if (incomingBlob.Length == 0) // initialization step
            {
                byte[]? outgoingBlob = await _krbLibAuth.InitiateAuthentication();
                //TODO: Calculate statusCode?
                return (outgoingBlob, NegotiateAuthenticationStatusCode.ContinueNeeded);
            }
            else // next step
            {
                //TODO: Don't Ignore if the incoming message has more stuff that need communicating.
                _krbLibAuth.DecodeAndDecryptGssApiToken(incomingBlob);
                return (null, NegotiateAuthenticationStatusCode.Completed);
            }
        }
        else
        {
            if (Interlocked.CompareExchange(ref _state, (int)State.InProgress, (int)State.Idle) != (int)State.Idle)
            {
                throw new InvalidOperationException($"Cannot {nameof(GetOutgoingBlobAsync)} when {_state}.");
            }
        
            Task<(byte[]? outgoingBlob, NegotiateAuthenticationStatusCode)> result = Task.Run(() =>
            {
                try
                {
                    byte[]? outgoingBlob = _negotiateAuthentication.GetOutgoingBlob(incomingBlob, out NegotiateAuthenticationStatusCode statusCode);
                    return (outgoingBlob, statusCode);
                }
                finally
                {
                    State previous = (State)Interlocked.CompareExchange(ref _state, (int)State.Idle, (int)State.InProgress);
                    // When an async operation is cancelled, Dispose may have been called already
                    // and we're responsible for disposing the NegotiateAuthentication.
                    if (previous == State.DisposeRequested)
                    {
                        _negotiateAuthentication.Dispose();
                    }
                }
            });

            await result.WaitAsync(cancellationToken).ConfigureAwait(false);

            return await result.ConfigureAwait(false);
        }
    }


    public void ComputeIntegrityCheck(ReadOnlySpan<byte> message, System.Buffers.IBufferWriter<byte> signatureWriter)
    {
        if (KrbLibSim.ENABLED)
        {
            byte[] mic_message = _krbLibAuth.Get_MIC(message.ToArray());
            signatureWriter.Write(mic_message);
        }
        else
        {
#if NET8_0
        GetMICMethod(_negotiateAuthentication, message, signatureWriter);
#else
            _negotiateAuthentication.ComputeIntegrityCheck(message, signatureWriter);
#endif
        }
    }
}