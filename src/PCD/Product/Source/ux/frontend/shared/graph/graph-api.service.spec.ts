import { TestSpec, SpyCache } from "../../shared-tests/spec.base";
import { IAjaxService } from "../ajax.service";
import { IGraphApiService } from "./graph-api.service";

import "./graph-api.service";

describe("Graph API service", () => {
    let graphApiService: IGraphApiService;
    let ajaxServiceMock: SpyCache<IAjaxService>;

    beforeEach(() => {
        let spec = new TestSpec({
            returnMockedAjaxService: true,
            mockedAjaxServiceOptions: {
                authTokenManager: null
            }
        });
        ajaxServiceMock = spec.ajaxServiceMock;

        inject((_graphApiService_: IGraphApiService) => {
            graphApiService = _graphApiService_;
        });
    });

    it("gets list of all groups based on prefix", () => {
        ajaxServiceMock.getFor("get").and.stub();
        graphApiService.getAllGroupsWithFilter("OSG", "displayName");

        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "https://graph.microsoft.com/v1.0/groups?$filter=startswith(displayName%2C%20'OSG')",
            serviceName: "MsGraphService",
            operationName: "getAllGroupsWithFilter"
        });
    });

    it("gets list of all applications based on prefix", () => {
        ajaxServiceMock.getFor("get").and.stub();
        graphApiService.getAllApplicationsWithFilter("pdm", "displayName");

        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "https://graph.microsoft.com/beta/applications?$filter=startswith(displayName%2C%20'pdm')",
            serviceName: "MsGraphService",
            operationName: "getAllApplicationsWithFilter"
        });
    });

    it("gets list of all users based on prefix", () => {
        ajaxServiceMock.getFor("get").and.stub();

        graphApiService.getAllUsersWithFilter("Jessica", "displayName");

        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "https://graph.microsoft.com/v1.0/users?$filter=startswith(displayName%2C%20'Jessica')",
            serviceName: "MsGraphService",
            operationName: "getAllUsersWithFilter"
        });
    });

    it("correctly encodes the prefix", () => {
        ajaxServiceMock.getFor("get").and.stub();

        graphApiService.getAllUsersWithFilter("My #example!", "userPrincipalName");

        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "https://graph.microsoft.com/v1.0/users?$filter=startswith(userPrincipalName%2C%20'My%20%23example!')",
            serviceName: "MsGraphService",
            operationName: "getAllUsersWithFilter"
        });

        graphApiService.getAllGroupsWithFilter("\";,/?:@&=+$ -_.!~*'()", "displayName");

        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "https://graph.microsoft.com/v1.0/groups?$filter=startswith(displayName%2C%20'%22%3B%2C%2F%3F%3A%40%26%3D%2B%24%20-_.!~*'()')",
            serviceName: "MsGraphService",
            operationName: "getAllGroupsWithFilter"
        });
    });
});
