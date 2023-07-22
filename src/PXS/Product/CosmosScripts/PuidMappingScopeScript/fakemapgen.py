from random import *
import os
path = "./local/puidmapping/PROD/PuidMapping/2017/12/24"
existingPath = "./local/puidmapping/PROD/ExistingAccounts"
filepath = path + "/puidmapwcid_06.csv"
os.makedirs(path)
os.makedirs(existingPath)
fakecsv = open(filepath, 'w')
writeFake = True
for i in range (0, 544):
    existingFile = existingPath + "/FSS" + str('%03d' % i) + ".csv"
    existingFakeCsv = open(existingFile, 'w')
    for x in range(0, 100):
        puid = randint(800000000000000,900000000000000)
        cid = randint(-900000000000000,900000000000000)
        anid = format(randint(-900000000000000,900000000000000), 'x')
        opid = "OpidIamYouSeew2f3d4865f"
        if writeFake:
            fakecsv.write(str(puid) + ',' + anid + ',' + opid + ',' + str(cid) + '\n')
        existingFakeCsv.write(str(puid) + ',' + anid + ',' + opid + ',' + str(cid) + '\n')
    writeFake = False
fakecsv.close()

