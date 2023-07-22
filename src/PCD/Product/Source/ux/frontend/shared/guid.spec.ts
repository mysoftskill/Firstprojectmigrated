import * as Guid from "./guid";

describe("guid", () => {
    describe("isValidGuid", () => {
        it("returns false on falsy input", () => {
            expect(Guid.isValidGuid("")).toBe(false);
            expect(Guid.isValidGuid(null)).toBe(false);
            expect(Guid.isValidGuid(undefined)).toBe(false);
        });

        it("returns false on bad input", () => {
            expect(Guid.isValidGuid("invalid string")).toBe(false);
            expect(Guid.isValidGuid("CAD725F2-76434235-9F4F-02F3DFC5A53")).toBe(false);
            expect(Guid.isValidGuid("CAD725F2-7643-4235-9F4F-02F3DFC5A53CA")).toBe(false);
            expect(Guid.isValidGuid("CAD725F2-7643-4235-9F4F-02F3DFC5A53z")).toBe(false);
            expect(Guid.isValidGuid(" {CAD725F2-7643-4235-9F4F-02F3DFC5A53C}")).toBe(false);
            expect(Guid.isValidGuid("{CAD725F2-7643-4235-9F4F-02F3DFC5A53C} ")).toBe(false);
        });

        it("returns false on unbalanced brackets", () => {
            expect(Guid.isValidGuid("CAD725F2-7643-4235-9F4F-02F3DFC5A53C}")).toBe(false);
            expect(Guid.isValidGuid("{CAD725F2-7643-4235-9F4F-02F3DFC5A53C")).toBe(false);
            expect(Guid.isValidGuid("{CAD725F2-7643-4235-9F4F-02F3DFC5A53C}}")).toBe(false);
            expect(Guid.isValidGuid("{{CAD725F2-7643-4235-9F4F-02F3DFC5A53C}")).toBe(false);
        });

        it("returns true on correct GUIDs", () => {
            expect(Guid.isValidGuid("{CAD725F2-7643-4235-9F4F-02F3DFC5A53C}")).toBe(true);
            expect(Guid.isValidGuid("CAD725F2-7643-4235-9F4F-02F3DFC5A53C")).toBe(true);
        });
    });

    describe("generateFuzzyGuidFrom", () => {
        let base = "00000000-0000-0000-0000-000000000000";

        it("returns base Guid on falsy input", () => {
            expect(Guid.generateFuzzyGuidFrom(null)).toBe(base);
            expect(Guid.generateFuzzyGuidFrom(undefined)).toBe(base);
        });

        it("returns correct fuzzy Guid for valid input", () => {
            expect(Guid.generateFuzzyGuidFrom(0)).toBe(base);

            expect(Guid.generateFuzzyGuidFrom(1)).toBe("00000000-0000-0000-0000-000000000001");
            expect(Guid.generateFuzzyGuidFrom(21)).toBe("00000000-0000-0000-0000-000000000021");
            expect(Guid.generateFuzzyGuidFrom(321)).toBe("00000000-0000-0000-0000-000000000321");
            expect(Guid.generateFuzzyGuidFrom(4321)).toBe("00000000-0000-0000-0000-000000004321");

            expect(Guid.generateFuzzyGuidFrom(123456789123)).toBe("00000000-0000-0000-0000-123456789123");
            expect(Guid.generateFuzzyGuidFrom(1234567891234)).toBe(base);
        });
    });
});
