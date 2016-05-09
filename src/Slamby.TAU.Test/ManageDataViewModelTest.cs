using System;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Slamby.SDK.Net.Managers;
using Slamby.SDK.Net.Models;
using Slamby.TAU.Enum;
using Slamby.TAU.Helper;
using Slamby.TAU.Model;
using Slamby.TAU.ViewModel;

namespace Slamby.TAU.Test
{
    [TestClass]
    public class ManageDataViewModelTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
            GlobalStore.IsInTestMode = true;
            GlobalStore.EndpointConfiguration.ApiBaseEndpoint=new Uri("http://tautest/");
        }


        [TestMethod]
        public async Task AddTagTest()
        {
            var dialogHandler = new TestDialogHandler();
            dialogHandler.SetTestResult(CommonDialogResult.Ok);
            var guid = Guid.NewGuid().ToString();
            dialogHandler.SetTestInput(new JContent(new Tag { Id = guid, Name = guid, ParentId = null }));
            var vm = new ManageDataViewModel(new DataSet { Name = "unit_test" }, dialogHandler);
            await vm.AddTag();
        }

        //[TestMethod]
        //public async Task AddDocumentTest()
        //{
        //    GlobalStore.IsInTestMode = true;
        //    GlobalStore.EndpointConfiguration.ApiBaseEndpoint = new Uri("http://localhost:29689/");
        //    DialogHandler.TestResult = CommonDialogResult.Ok;
        //    DialogHandler.TestInput = new JContent(new { ad_id = "testid1", subject = "testsubject1", body = "test body1", category = "testid" });
        //    var vm = new ManageDataViewModel();
        //    Messenger.Default.Send(new UpdateMessage(UpdateType.SelectedDataSetChange, new DataSet { Name = "unit_test" }));
        //    await vm.AddDocument();
        //}
    }
}
