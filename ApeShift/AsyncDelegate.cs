using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace ApeShift
{
  internal class AsyncDelegate
  {

    private string _SendToApePubKeyQ;

    private ApeSession _ExplorerSession;

    private MoneroRequest _Request;

    private string _ContextToken { get; set; }
    private event MoneroResponseCompletedEventHandler TaskCompleted;
    public AsyncDelegate(ApeSession ExplorerSession, MoneroRequest Request, string SendToPubKey)
    {
      _ContextToken = Guid.NewGuid().ToString();
      _ExplorerSession = ExplorerSession;
      _SendToApePubKeyQ = SendToPubKey;
      _Request = Request;
    }
    void Processor_Exp(MoneroResponseCompletedEventArgs Resp)
    {
      if (Resp.Response.Context == _ContextToken)
        TaskCompleted.Invoke(Resp);
    }
    public async Task<MoneroResponse<object>> RequestAsync(int TimeoutSeconds)
    {
      using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds)))
      {
        var tcs = new TaskCompletionSource<MoneroResponseCompletedEventArgs>();
        TaskCompleted += (e) => CompletedEvent(tcs, e, () => e);

        cancellationTokenSource.Token.Register(() =>
        {
          tcs.TrySetCanceled();
        });

        var send = await _ExplorerSession.SendMessage(_Request, _SendToApePubKeyQ, _ContextToken);

        if (!send)
          return new MoneroResponse<object>() { ErrorMessage = "Error, invalid request or channel.", };

        _ExplorerSession._OnResponse += Processor_Exp;

        var task = await tcs.Task;

        _ExplorerSession._OnResponse -= Processor_Exp;

        return task.Response;
      }
    }
    void CompletedEvent<T>(TaskCompletionSource<T> tcs, AsyncCompletedEventArgs e, Func<T> getResult)
    {
      try
      {
        if (e.Error != null) tcs.TrySetException(e.Error);
        else if (e.Cancelled) tcs.TrySetCanceled();
        else tcs.TrySetResult(getResult());
      }
      catch (Exception)
      {
        tcs.TrySetCanceled();
      }
    }

  }
}
