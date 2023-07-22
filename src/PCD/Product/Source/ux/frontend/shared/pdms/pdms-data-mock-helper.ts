import * as Pdms from "../pdms/pdms-types";
import { DataOwner } from "../pdms/pdms-types";

export class PdmsDataMockHelper {

    constructor(
        private readonly $promises: ng.IQService) { }

    public createNFakeTeamsFn(n: number): () => ng.IPromise<DataOwner[]> {
        return () => {
            let fakeTeams = _.range(n).reverse().map(idx => {
                const firstLetterIdx = idx % 26;

                return <Pdms.DataOwner> {
                    id: `I9n_Team${idx}_Id`,
                    name: `I9n_Team${String.fromCharCode(firstLetterIdx + 65)}_Name`,
                    description: "",
                    alertContacts: [],
                    announcementContacts: [],
                    writeSecurityGroups: []
                };
            });

            return this.$promises.resolve(fakeTeams);
        };
    }
}
