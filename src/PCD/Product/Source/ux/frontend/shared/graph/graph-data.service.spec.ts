import * as angular from "angular";
import { TestSpec, SpyCache } from "../../shared-tests/spec.base";

import * as GraphTypes from "./graph-types";
import { IGraphApiService } from "./graph-api.service";
import { IGraphDataService } from "./graph-data.service";

describe("Graph data service", () => {
    let spec: TestSpec;
    let graphDataService: IGraphDataService;
    let graphApiServiceMock: SpyCache<IGraphApiService>;
    let $q: ng.IQService;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_graphApiService_: IGraphApiService, _graphDataService_: IGraphDataService, _$q_: ng.IQService) => {
            graphApiServiceMock = new SpyCache(_graphApiService_);
            graphDataService = _graphDataService_;
            $q = _$q_;
        });
    });

    it("gets a list of security groups filtered by prefix name", (done: DoneFn) => {
        // arrange
        let response: any = {
            value: [{
                    "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                    "displayName": "OSG_org_fte_redmond",
                    "mail": "id1-email@microsoft.com",
                    "securityEnabled": true,
                }, {
                    "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                    "displayName": "OSG_OSGExtRO-18822",
                    "mail": "id2-email@microsoft.com",
                    "securityEnabled": true,
                }, {
                    "id": "a33a387c-d951-455f-ab06-fce163771fa8",
                    "displayName": "OSG_dude",
                    "mail": "id3-email@microsoft.com",
                    "securityEnabled": false,
                }
            ]
        };
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Group[]>(response));

        let expectedGroupsFromApiCall: GraphTypes.Group[] = [{
            "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
            "displayName": "OSG_org_fte_redmond",
            "securityEnabled": true,
            "email": "id1-email@microsoft.com",
            "isInvalid": false,
        }, {
            "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
            "displayName": "OSG_OSGExtRO-18822",
            "securityEnabled": true,
            "email": "id2-email@microsoft.com",
            "isInvalid": false,
        }];

        // act
        graphDataService.getSecurityGroupsWithPrefix("osg")
            .then((retrievedGroupsFromApiCall: GraphTypes.Group[]) => {

                // assert
                expect(retrievedGroupsFromApiCall).toEqual(expectedGroupsFromApiCall);

                done();
            });
        spec.runDigestCycle();
    });

    it("gets a list of security groups for exact match of group id", (done: DoneFn) => {
        // arrange
        let response: any = {
            value: [{
                    "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                    "displayName": "OSG_org_fte_redmond",
                    "mail": "id1-email@microsoft.com",
                    "securityEnabled": true,
                }
            ]
        };
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Group[]>(response));

        let expectedGroupsFromApiCall: GraphTypes.Group[] = [{
            "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
            "displayName": "OSG_org_fte_redmond",
            "securityEnabled": true,
            "email": "id1-email@microsoft.com",
            "isInvalid": false,
        }];

        // act
        graphDataService.getSecurityGroupsWithPrefix("b4d4d2cf-23c8-4568-bde8-7a976331f547")
            .then((retrievedGroupsFromApiCall: GraphTypes.Group[]) => {

                // assert
                expect(retrievedGroupsFromApiCall).toEqual(expectedGroupsFromApiCall);

                done();
            });
        spec.runDigestCycle();
    });

    it("gets an empty list if display name prefix does not match AND exact Id does not match", (done: DoneFn) => {
        // arrange
        let response: any = {
            value: []
        };
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Group[]>(response));

        let expectedGroupsFromApiCall: GraphTypes.Group[] = [];

        // act
        graphDataService.getSecurityGroupsWithPrefix("somethingDefinitelyNotPresent")
            .then((retrievedGroupsFromApiCall: GraphTypes.Group[]) => {

                // assert
                expect(retrievedGroupsFromApiCall).toEqual(expectedGroupsFromApiCall);

                done();
            });
        spec.runDigestCycle();
    });

    it("finds a group from cache based on name", (done: DoneFn) => {
        // arrange
        let groupsResponse: any = {
            value: [{
                    "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                    "displayName": "OSG_org_fte_redmond",
                    "mail": "id1-mail@microsoft.com",
                    "securityEnabled": true,
                }, {
                    "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                    "displayName": "OSG_OSGExtRO-18822",
                    "mail": "id2-mail@microsoft.com",
                    "securityEnabled": true,
                }
            ]
        };
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Group[]>(groupsResponse));
        graphDataService.getSecurityGroupsWithPrefix("OSG") // to populate the cache
            .then(() => {

                // act
                let group: GraphTypes.Group = graphDataService.getSecurityGroupFromCache("OSG_org_fte_redmond");

                // assert
                expect(group.id).toEqual("b4d4d2cf-23c8-4568-bde8-7a976331f547");
                expect(group.email).toEqual("id1-mail@microsoft.com");

                done();
            });
        spec.runDigestCycle();
    });

    it("finds a group from cache based on name if not present", (done: DoneFn) => {
        // arrange
        let groupsResponse: any = {
            value: [{
                    "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                    "displayName": "OSG_org_fte_redmond",
                    "mail": "id1-mail@microsoft.com",
                    "securityEnabled": true,
                }, {
                    "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                    "displayName": "OSG_OSGExtRO-18822",
                    "mail": "id2-mail@microsoft.com",
                    "securityEnabled": true,
                }
            ]
        };
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Group[]>(groupsResponse));
        graphDataService.getSecurityGroupsWithPrefix("OSG") // to populate the cache
            .then(() => {

                // act
                let group: GraphTypes.Group = graphDataService.getSecurityGroupFromCache("Name_not_present");

                // assert
                expect(group).toBeUndefined();

                done();
            });
        spec.runDigestCycle();
    });

    it("determines if a security group display name is valid", (done: DoneFn) => {
        // arrange
        let groupsResponse: any = {
            value: [{
                    "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                    "displayName": "OSG_org_fte_redmond",
                    "securityEnabled": true,
                }, {
                    "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                    "displayName": "OSG_OSGExtRO-18822",
                    "securityEnabled": true,
                }
            ]
        };
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Group[]>(groupsResponse));
        graphDataService.getSecurityGroupsWithPrefix("OSG") // to populate the cache
            .then(() => {

                // act
                let isValid: boolean = graphDataService.isSecurityGroupNameValid("OSG_org_fte_redmond");

                // assert
                expect(isValid).toEqual(true);

                done();
            });
        spec.runDigestCycle();
    });

    it("determines if a security group display name is invalid", (done: DoneFn) => {
        // arrange
        let groupsResponse: any = {
            value: [{
                    "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                    "displayName": "OSG_org_fte_redmond",
                    "securityEnabled": true,
                }, {
                    "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                    "displayName": "OSG_OSGExtRO-18822",
                    "securityEnabled": true,
                }
            ]
        };
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Group[]>(groupsResponse));
        graphDataService.getSecurityGroupsWithPrefix("OSG") // to populate the cache
            .then(() => {

                // act
                let isValid: boolean = graphDataService.isSecurityGroupNameValid("somethingDefinitelyNotValid");

                // assert
                expect(isValid).toEqual(false);

                done();
            });
        spec.runDigestCycle();
    });

    it("gets a list of applications filtered by prefix name", (done: DoneFn) => {
        // arrange
        let response: any = {
            value: [{
                "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                "displayName": "OSG_org_fte_redmond"
            }, {
                "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                "displayName": "OSG_OSGExtRO-18822"
            }
            ]
        };
        graphApiServiceMock.getFor("getAllApplicationsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Application[]>(response));

        let expectedApplicationsFromApiCall: GraphTypes.Application[] = [{
            "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
            "displayName": "OSG_org_fte_redmond",
            "isInvalid": false,
        }, {
            "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
            "displayName": "OSG_OSGExtRO-18822",
            "isInvalid": false,
        }];

        // act
        graphDataService.getApplicationsWithPrefix("osg")
            .then((retrievedAppsFromApiCall: GraphTypes.Application[]) => {

                // assert
                expect(retrievedAppsFromApiCall).toEqual(expectedApplicationsFromApiCall);

                done();
            });
        spec.runDigestCycle();
    });

    it("gets a list of applications for exact match of application id", (done: DoneFn) => {
        // arrange
        let response: any = {
            value: [{
                "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                "displayName": "OSG_org_fte_redmond"
            }
            ]
        };
        graphApiServiceMock.getFor("getAllApplicationsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Application[]>(response));

        let expectedApplicationsFromApiCall: GraphTypes.Application[] = [{
            "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
            "displayName": "OSG_org_fte_redmond",
            "isInvalid": false,
        }];

        // act
        graphDataService.getApplicationsWithPrefix("b4d4d2cf-23c8-4568-bde8-7a976331f547")
            .then((retrievedAppsFromApiCall: GraphTypes.Application[]) => {

                // assert
                expect(retrievedAppsFromApiCall).toEqual(expectedApplicationsFromApiCall);

                done();
            });
        spec.runDigestCycle();
    });

    it("gets an empty list if display name prefix does not match AND exact Id does not match", (done: DoneFn) => {
        // arrange
        let response: any = {
            value: []
        };
        graphApiServiceMock.getFor("getAllApplicationsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Application[]>(response));

        let expectedAppsFromApiCall: GraphTypes.Application[] = [];

        // act
        graphDataService.getApplicationsWithPrefix("somethingDefinitelyNotPresent")
            .then((retrievedAppsFromApiCall: GraphTypes.Application[]) => {

                // assert
                expect(retrievedAppsFromApiCall).toEqual(expectedAppsFromApiCall);

                done();
            });
        spec.runDigestCycle();
    });

    it("finds an application from cache based on name", (done: DoneFn) => {
        // arrange
        let appsResponse: any = {
            value: [{
                "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                "displayName": "OSG_org_fte_redmond"
            }, {
                "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                "displayName": "OSG_OSGExtRO-18822"
            }
            ]
        };
        graphApiServiceMock.getFor("getAllApplicationsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Application[]>(appsResponse));
        graphDataService.getApplicationsWithPrefix("OSG") // to populate the cache
            .then(() => {

                // act
                let app: GraphTypes.Application = graphDataService.getApplicationFromCache("OSG_org_fte_redmond");

                // assert
                expect(app.id).toEqual("b4d4d2cf-23c8-4568-bde8-7a976331f547");

                done();
            });
        spec.runDigestCycle();
    });

    it("finds an application from cache based on name if not present", (done: DoneFn) => {
        // arrange
        let appsResponse: any = {
            value: [{
                "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                "displayName": "OSG_org_fte_redmond"
            }, {
                "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                "displayName": "OSG_OSGExtRO-18822"
            }
            ]
        };
        graphApiServiceMock.getFor("getAllApplicationsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Application[]>(appsResponse));
        graphDataService.getApplicationsWithPrefix("OSG") // to populate the cache
            .then(() => {

                // act
                let app: GraphTypes.Application = graphDataService.getApplicationFromCache("Name_not_present");

                // assert
                expect(app).toBeUndefined();

                done();
            });
        spec.runDigestCycle();
    });

    it("determines if an applicaiton display name is valid", (done: DoneFn) => {
        // arrange
        let appsResponse: any = {
            value: [{
                "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                "displayName": "OSG_org_fte_redmond"
            }, {
                "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                "displayName": "OSG_OSGExtRO-18822"
            }
            ]
        };
        graphApiServiceMock.getFor("getAllApplicationsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Application[]>(appsResponse));
        graphDataService.getApplicationsWithPrefix("OSG") // to populate the cache
            .then(() => {

                // act
                let isValid: boolean = graphDataService.isApplicationNameValid("OSG_org_fte_redmond");

                // assert
                expect(isValid).toEqual(true);

                done();
            });
        spec.runDigestCycle();
    });

    it("determines if an application display name is invalid", (done: DoneFn) => {
        // arrange
        let appsResponse: any = {
            value: [{
                "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                "displayName": "OSG_org_fte_redmond"
            }, {
                "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                "displayName": "OSG_OSGExtRO-18822"
            }
            ]
        };
        graphApiServiceMock.getFor("getAllApplicationsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Application[]>(appsResponse));
        graphDataService.getApplicationsWithPrefix("OSG") // to populate the cache
            .then(() => {

                // act
                let isValid: boolean = graphDataService.isApplicationNameValid("somethingDefinitelyNotValid");

                // assert
                expect(isValid).toEqual(false);

                done();
            });
        spec.runDigestCycle();
    });

    it("gets a list of contacts filtered by name", (done: DoneFn) => {
        // arrange
        let response: any = {
            value: [{
                    "id": "ID1",
                    "displayName": "Jessica Hunt",
                    "mail": "jessicah@microsoft.com",
                }, {
                    "id": "ID2",
                    "displayName": "Jessica Simpson",
                    "mail": "jessicas@microsoft.com",
                }
            ]
        };
        graphApiServiceMock.getFor("getAllUsersWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.User[]>(response));
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.User[]>([]));

        let expectedGroupsFromApiCall: GraphTypes.Contact[] = [{
                "id": "ID1",
                "displayName": "Jessica Hunt",
                "email": "jessicah@microsoft.com",
                "isInvalid": false,
            }, {
                "id": "ID2",
                "displayName": "Jessica Simpson",
                "email": "jessicas@microsoft.com",
                "isInvalid": false,
            }
        ];

        // act
        graphDataService.getContactsWithPrefix("Jessica")
            .then((retrievedGroupsFromApiCall: GraphTypes.Contact[]) => {

                // assert
                expect(retrievedGroupsFromApiCall).toEqual(expectedGroupsFromApiCall);

                done();
            });
        spec.runDigestCycle();
    });

    it("gets a list of contacts and dedupes them based on ID", (done: DoneFn) => {
        // arrange
        let response: any = {
            value: [{
                    "id": "ID1",
                    "displayName": "Jessica Hunt",
                    "mail": "jessica.hunt@microsoft.com",
                    "userPrincipalName": "jessicah@microsoft.com"
                }
            ]
        };

        // Note that this will be called twice and results will be queued.
        graphApiServiceMock.getFor("getAllUsersWithFilter").and.returnValue(spec.asHttpPromise<any[]>(response));
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<any[]>([]));

        let expectedGroupsFromApiCall: GraphTypes.Contact[] = [{
                "id": "ID1",
                "displayName": "Jessica Hunt",
                "email": "jessicah@microsoft.com",
                "isInvalid": false,
            }
        ];

        // act
        graphDataService.getContactsWithPrefix("Jessica")
            .then((retrievedContactsFromApiCall: GraphTypes.Contact[]) => {

                // assert
                expect(retrievedContactsFromApiCall).toEqual(expectedGroupsFromApiCall);

                done();
            });
        spec.runDigestCycle();
    });

    it("gets a list of contacts filtered by email", (done: DoneFn) => {
        // arrange
        let response: any = {
            value: [{
                    "id": "ID1",
                    "displayName": "Jessica Hunt",
                    "mail": "jessicah@microsoft.com",
                }, {
                    "id": "ID2",
                    "displayName": "Jessica Simpson",
                    "mail": "jessicas@microsoft.com",
                }
            ]
        };
        graphApiServiceMock.getFor("getAllUsersWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.User[]>(response));
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.User[]>([]));

        let expectedGroupsFromApiCall: GraphTypes.Contact[] = [{
                "id": "ID1",
                "displayName": "Jessica Hunt",
                "email": "jessicah@microsoft.com",
                "isInvalid": false,
            }, {
                "id": "ID2",
                "displayName": "Jessica Simpson",
                "email": "jessicas@microsoft.com",
                "isInvalid": false,
            }
        ];

        // act
        graphDataService.getContactsWithPrefix("jessicah@microsoft.com")
            .then((retrievedGroupsFromApiCall: GraphTypes.Contact[]) => {

                // assert
                expect(retrievedGroupsFromApiCall).toEqual(expectedGroupsFromApiCall);

                done();
            });
        spec.runDigestCycle();
    });

    it("determines the id of a contact based on name", (done: DoneFn) => {
        // arrange
        let usersResponse: any = {
            value: [{
                    "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                    "displayName": "Jack",
                    "email": "jack@microsoft.com",
                    "isInvalid": false,
                }, {
                    "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                    "displayName": "Jessica",
                    "email": "jessicah@microsoft.com",
                    "isInvalid": false,
                }
            ]
        };
        graphApiServiceMock.getFor("getAllUsersWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.User[]>(usersResponse));
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.User[]>([]));
        
        graphDataService.getContactsWithPrefix("James") // to populate the cache
            .then(() => {

                // act
                let contact: GraphTypes.Contact = graphDataService.getContactFromCache("Jessica");

                // assert
                expect(contact.id).toEqual("82eda70a-0b6c-4a53-ab77-9b62c646677b");

                done();
            });
        spec.runDigestCycle();
    });

    it("determines the id of a group based on name if not present", (done: DoneFn) => {
        // arrange
        let usersResponse: any = {
            value: [{
                    "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                    "displayName": "Jack",
                    "email": "jack@microsoft.com",
                    "isInvalid": false,
                }, {
                    "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                    "displayName": "Jessica",
                    "email": "jessicah@microsoft.com",
                    "isInvalid": false,
                }
            ]
        };
        graphApiServiceMock.getFor("getAllUsersWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.User[]>(usersResponse));
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.User[]>([]));
        
        graphDataService.getContactsWithPrefix("James") // to populate the cache
            .then(() => {

                // act
                let contact: GraphTypes.Contact = graphDataService.getContactFromCache("Name_not_present");

                // assert
                expect(contact).toBeUndefined();

                done();
            });
        spec.runDigestCycle();
    });

    it("determines if a contact display name is valid", (done: DoneFn) => {
        // arrange
        let usersResponse: any = {
            value: [{
                    "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                    "displayName": "Jack",
                    "email": "jack@microsoft.com",
                    "isInvalid": false,
                }, {
                    "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                    "displayName": "Jessica",
                    "email": "jessicah@microsoft.com",
                    "isInvalid": false,
                }
            ]
        };
        graphApiServiceMock.getFor("getAllUsersWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.User[]>(usersResponse));
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.User[]>([]));
        
        graphDataService.getContactsWithPrefix("James") // to populate the cache
            .then(() => {

                // act
                let isValid: boolean = graphDataService.isContactNameValid("Jack");

                // assert
                expect(isValid).toEqual(true);

                done();
            });
        spec.runDigestCycle();
    });

    it("determines if a contact display name is invalid", (done: DoneFn) => {
        // arrange
        let usersResponse: any = {
            value: [{
                    "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                    "displayName": "Jack",
                    "email": "jack@microsoft.com",
                    "isInvalid": false,
                }, {
                    "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                    "displayName": "Jessica",
                    "email": "jessicah@microsoft.com",
                    "isInvalid": false,
                }
            ]
        };
        graphApiServiceMock.getFor("getAllUsersWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.User[]>(usersResponse));
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.User[]>([]));
        
        graphDataService.getContactsWithPrefix("James") // to populate the cache
            .then(() => {

                // act
                let isValid: boolean = graphDataService.isContactNameValid("somethingDefinitelyNotValid");

                // assert
                expect(isValid).toEqual(false);

                done();
            });
        spec.runDigestCycle();
    });

    it("gets a contact through email", (done: DoneFn) => {
        // arrange
        let usersResponse: any = {
            value: [{
                    "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                    "displayName": "Jack",
                    "email": "jack@microsoft.com",
                    "isInvalid": false,
                }, {
                    "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                    "displayName": "Jessica",
                    "email": "jessicah@microsoft.com",
                    "isInvalid": false,
                }
            ]
        };
        graphApiServiceMock.getFor("getAllUsersWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Contact[]>(usersResponse));
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Contact[]>([]));

        // act
        graphDataService.getContactByEmail("jack@microsoft.com")
            .then((contact: GraphTypes.Contact) => {

                // assert
                expect(contact.displayName).toEqual("Jack");
                expect(contact.id).toEqual("b4d4d2cf-23c8-4568-bde8-7a976331f547");

                done();
            });
        spec.runDigestCycle();
    });

    it("gets a contact through email in dot format", (done: DoneFn) => {
        // arrange
        let usersResponse: any = {
            data : {
                value: [{
                        "id": "b4d4d2cf-23c8-4568-bde8-7a976331f547",
                        "displayName": "Jack Boring",
                        "email": "jack.boring@microsoft.com",
                        "userPrincipalName": "jackb@microsoft.com",
                        "isInvalid": false,
                    }, {
                        "id": "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                        "displayName": "Jessica Hunt",
                        "email": "jessica.hunt@microsoft.com",
                        "userPrincipalName": "jessicah@microsoft.com",
                        "isInvalid": false,
                    }
                ]
            }
        };
        graphApiServiceMock.getFor("getAllUsersWithFilter").and.callFake((prefixStr: string, filterStr: string) => {
            if (filterStr === "mail") {
                return $q.resolve(usersResponse);
            } else {
                return $q.resolve([]);
            }
        });
        graphApiServiceMock.getFor("getAllGroupsWithFilter").and.returnValue(spec.asHttpPromise<GraphTypes.Contact[]>([]));

        // act
        graphDataService.getContactByEmail("jack.boring@microsoft.com")
            .then((contact: GraphTypes.Contact) => {

                // assert
                expect(contact.displayName).toEqual("Jack Boring");
                expect(contact.id).toEqual("b4d4d2cf-23c8-4568-bde8-7a976331f547");

                done();
            });
        spec.runDigestCycle();
    });
});
