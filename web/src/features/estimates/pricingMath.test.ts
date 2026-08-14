import { describe, expect, it } from "vitest";
import { areaSquareFeet, billableSquareFeet, estimateTotals, roomPreview } from "./pricingMath";

describe("areaSquareFeet", () => {
  it("rounds each dimension up to whole feet before multiplying", () => {
    // 10.1 x 10.1 bills as 11 x 11, not as 102.01.
    expect(areaSquareFeet(10.1, 10.1)).toBe(121);
    expect(areaSquareFeet(10, 10)).toBe(100);
    expect(areaSquareFeet(20, 15)).toBe(300);
  });

  it("treats missing or nonsensical dimensions as zero rather than NaN", () => {
    expect(areaSquareFeet(0, 10)).toBe(0);
    expect(areaSquareFeet(-5, 10)).toBe(0);
    expect(areaSquareFeet(Number.NaN, 10)).toBe(0);
  });
});

describe("billableSquareFeet", () => {
  it("applies the waste allowance to the rounded area", () => {
    expect(billableSquareFeet(20, 15, 10)).toBe(330);
    expect(billableSquareFeet(10, 10, 0)).toBe(100);
    expect(billableSquareFeet(10, 10, 12.5)).toBe(112.5);
  });
});

describe("roomPreview", () => {
  it("prices a room from its billable area and rates", () => {
    const preview = roomPreview({
      lengthFeet: 20,
      widthFeet: 15,
      wastePercentage: 10,
      laborRatePerSqFt: 4,
      materialRatePerSqFt: 1.25,
    });

    expect(preview.billableSquareFeet).toBe(330);
    expect(preview.laborCost).toBe(1320);
    expect(preview.materialCost).toBe(412.5);
    expect(preview.roomTotal).toBe(1732.5);
  });

  it("returns zeroes for an untouched room instead of NaN", () => {
    const preview = roomPreview({
      lengthFeet: 0,
      widthFeet: 0,
      wastePercentage: 10,
      laborRatePerSqFt: 0,
      materialRatePerSqFt: 0,
    });

    expect(preview).toEqual({
      billableSquareFeet: 0,
      laborCost: 0,
      materialCost: 0,
      roomTotal: 0,
    });
  });
});

describe("estimateTotals", () => {
  it("matches the server's worked example", () => {
    const totals = estimateTotals(
      [
        {
          lengthFeet: 20,
          widthFeet: 15,
          wastePercentage: 10,
          laborRatePerSqFt: 4,
          materialRatePerSqFt: 1.25,
        },
      ],
      10,
    );

    expect(totals.laborSubtotal).toBe(1320);
    expect(totals.materialSubtotal).toBe(412.5);
    expect(totals.subtotal).toBe(1732.5);
    expect(totals.tax).toBe(173.25);
    expect(totals.total).toBe(1905.75);
  });

  it("sums across rooms", () => {
    const room = {
      lengthFeet: 10,
      widthFeet: 10,
      wastePercentage: 0,
      laborRatePerSqFt: 3,
      materialRatePerSqFt: 1,
    };

    const totals = estimateTotals([room, room, room], 0);

    expect(totals.subtotal).toBe(1200);
    expect(totals.tax).toBe(0);
    expect(totals.total).toBe(1200);
  });

  it("has no totals for an estimate with no rooms", () => {
    const totals = estimateTotals([], 8.25);

    expect(totals.subtotal).toBe(0);
    expect(totals.tax).toBe(0);
    expect(totals.total).toBe(0);
  });

  it("does not accumulate binary floating point drift", () => {
    // 0.1 + 0.2 !== 0.3 in float64. Summing a rate that is not representable in
    // binary across many rooms is where the naive implementation visibly diverged
    // from the server.
    const room = {
      lengthFeet: 1,
      widthFeet: 1,
      wastePercentage: 0,
      laborRatePerSqFt: 0.1,
      materialRatePerSqFt: 0.2,
    };

    const totals = estimateTotals(Array.from({ length: 10 }, () => room), 0);

    expect(totals.laborSubtotal).toBe(1);
    expect(totals.materialSubtotal).toBe(2);
    expect(totals.subtotal).toBe(3);
  });

  it("computes tax on a half-cent boundary the same way every time", () => {
    // Subtotal 100.05 at 5% is exactly 5.0025; float arithmetic can land either
    // side of the half-cent and flip how the UI renders it.
    const totals = estimateTotals(
      [
        {
          lengthFeet: 1,
          widthFeet: 1,
          wastePercentage: 0,
          laborRatePerSqFt: 100.05,
          materialRatePerSqFt: 0,
        },
      ],
      5,
    );

    expect(totals.subtotal).toBe(100.05);
    expect(totals.tax).toBe(5.0025);
    expect(totals.total).toBe(105.0525);
  });
});
