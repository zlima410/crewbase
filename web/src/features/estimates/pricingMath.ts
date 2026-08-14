const INPUT_UNIT = 10_000n;

const MONEY_UNIT = 10_000_000_000n;

export type RoomInput = {
  lengthFeet: number;
  widthFeet: number;
  wastePercentage: number;
  laborRatePerSqFt: number;
  materialRatePerSqFt: number;
};

export type RoomPreview = {
  billableSquareFeet: number;
  laborCost: number;
  materialCost: number;
  roomTotal: number;
};

function toFixedPoint(value: number): bigint {
  if (!Number.isFinite(value) || value <= 0) return 0n;

  return BigInt(Math.round(value * Number(INPUT_UNIT)));
}

function toNumber(value: bigint, unit: bigint): number {
  return Number(value / unit) + Number(value % unit) / Number(unit);
}

export function areaSquareFeet(lengthFeet: number, widthFeet: number): number {
  if (!Number.isFinite(lengthFeet) || !Number.isFinite(widthFeet)) return 0;
  if (lengthFeet <= 0 || widthFeet <= 0) return 0;

  return Math.ceil(lengthFeet) * Math.ceil(widthFeet);
}

function billableFixedPoint(lengthFeet: number, widthFeet: number, wastePercentage: number): bigint {
  const area = BigInt(areaSquareFeet(lengthFeet, widthFeet));

  const denominator = 100n * INPUT_UNIT;

  return (area * (denominator + toFixedPoint(wastePercentage)) * MONEY_UNIT) / denominator;
}

export function billableSquareFeet(
  lengthFeet: number,
  widthFeet: number,
  wastePercentage: number,
): number {
  return toNumber(billableFixedPoint(lengthFeet, widthFeet, wastePercentage), MONEY_UNIT);
}

export function roomPreview(room: RoomInput): RoomPreview {
  const billable = billableFixedPoint(room.lengthFeet, room.widthFeet, room.wastePercentage);
  const laborCost = (billable * toFixedPoint(room.laborRatePerSqFt)) / INPUT_UNIT;
  const materialCost = (billable * toFixedPoint(room.materialRatePerSqFt)) / INPUT_UNIT;

  return {
    billableSquareFeet: toNumber(billable, MONEY_UNIT),
    laborCost: toNumber(laborCost, MONEY_UNIT),
    materialCost: toNumber(materialCost, MONEY_UNIT),
    roomTotal: toNumber(laborCost + materialCost, MONEY_UNIT),
  };
}

export function estimateTotals(rooms: RoomInput[], taxRate: number) {
  let labor = 0n;
  let material = 0n;

  for (const room of rooms) {
    const billable = billableFixedPoint(room.lengthFeet, room.widthFeet, room.wastePercentage);

    labor += (billable * toFixedPoint(room.laborRatePerSqFt)) / INPUT_UNIT;
    material += (billable * toFixedPoint(room.materialRatePerSqFt)) / INPUT_UNIT;
  }

  const subtotal = labor + material;
  const tax = (subtotal * toFixedPoint(taxRate)) / (100n * INPUT_UNIT);

  return {
    laborSubtotal: toNumber(labor, MONEY_UNIT),
    materialSubtotal: toNumber(material, MONEY_UNIT),
    subtotal: toNumber(subtotal, MONEY_UNIT),
    taxRate,
    tax: toNumber(tax, MONEY_UNIT),
    total: toNumber(subtotal + tax, MONEY_UNIT),
  };
}

export const fmt = (value: number) =>
  value.toLocaleString(undefined, { style: "currency", currency: "USD" });
