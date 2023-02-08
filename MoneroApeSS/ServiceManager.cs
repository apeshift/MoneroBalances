using System;
using System.Collections.Generic;
using System.Threading;
using RokitFramework;
using MoneroApeTask;

namespace MoneroApeSS
{
  class ServiceManager : RokitFramework.ServiceManagerBase
  {

    DMNProcessor _DMNProcessor = null;
    List<WorkProcessor> _Processors = null;
    public ServiceManager(string instanceName)
      : base(instanceName)
    {

      _Processors = new List<WorkProcessor>();
      _DMNProcessor = new DMNProcessor(instanceName);
      _Processors.Add(_DMNProcessor);


      new Thread(() =>
      {

        CBAsyncManager.Start();

        ImportManager.Start();

      }).Start();

    }

    protected override List<RokitFramework.WorkProcessor> Processors
    {
      get { return _Processors; }
    }

    public override RokitFramework.IWorkQueue<T> GetWorkQueue<T>(string name)
    {
      return null;
    }
  }
}
