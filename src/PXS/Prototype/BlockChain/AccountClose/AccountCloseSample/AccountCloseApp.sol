pragma solidity ^0.4.20;

contract WorkbenchBase {
    event WorkbenchContractCreated(string applicationName, string workflowName, address originatingAddress);
    event WorkbenchContractUpdated(string applicationName, string workflowName, string action, address originatingAddress);

    string internal ApplicationName;
    string internal WorkflowName;

    function WorkbenchBase(string applicationName, string workflowName) internal {
        ApplicationName = applicationName;
        WorkflowName = workflowName;
    }

    function ContractCreated() internal {
        WorkbenchContractCreated(ApplicationName, WorkflowName, msg.sender);
    }

    function ContractUpdated(string action) internal {
        WorkbenchContractUpdated(ApplicationName, WorkflowName, action, msg.sender);
    }
}

contract AccountCloseApp is WorkbenchBase('AccountCloseApp', 'AccountCloseWorkflow') {

     //Set of States
    enum StateType { AccountCloseRequested, AccountCloseRequestProcessedBy3P, AccountCloseRequestAudited}

    //List of properties
    StateType public  State;
    address public  RequestorFirstParty;
    address public  Responder3P;
    address public Auditor;

    string public AadObjectId;
    string public AadTenantId;
    string public OrgIdPuid;
    string public ResponseMessage;

    // constructor function
    function AccountCloseApp(string aadObjectId, string aadTenantId, string orgIdPuid) public
    {
        RequestorFirstParty  = msg.sender;
        AadObjectId = aadObjectId;
        AadTenantId = aadTenantId;
        OrgIdPuid = orgIdPuid;        
        State = StateType.AccountCloseRequested;

        // call ContractCreated() to create an instance of this workflow
        ContractCreated();
    }

    // call this function to send a request
    function SendRequest(string aadObjectId, string aadTenantId, string orgIdPuid) public
    {
        if (RequestorFirstParty  != msg.sender)
        {
            revert();
        }

        AadObjectId = aadObjectId;
        AadTenantId = aadTenantId;
        OrgIdPuid = orgIdPuid;
        State = StateType.AccountCloseRequested;

        // call ContractUpdated() to record this action
        ContractUpdated('SendRequest');
    }

    // call this function to send a response
    function SendResponse(string responseMessage) public
    {
        Responder3P = msg.sender;

        // call ContractUpdated() to record this action
        ResponseMessage = responseMessage;
        State = StateType.AccountCloseRequestProcessedBy3P;
        ContractUpdated('SendResponse');
    }

    // call this function to send a response
    function AuditResponse(string auditResponseMessage) public
    {
        Auditor = msg.sender;

        // call ContractUpdated() to record this action
        ResponseMessage = auditResponseMessage;
        State = StateType.AccountCloseRequestAudited;
        ContractUpdated('AuditResponse');
    }
}