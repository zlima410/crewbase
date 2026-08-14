export const areaSquareFeet = (l: number, w: number) => Math.ceil(l) * Math.ceil(w);

export const billableSquareFeet = (l: number, w: number, wastePct: number) =>
  areaSquareFeet(l, w) * (1 + wastePct / 100);

export const roomTotals = (billable: number, laborRate: number, materialRate: number) => {
  const laborCost = billable * laborRate;
  const materialCost = billable * materialRate;
  return { laborCost, materialCost, roomTotal: laborCost + materialCost };
};

export const estimateTotals = (
  rooms: {
    lengthFeet: number;
    widthFeet: number;
    wastePercentage: number;
    laborRatePerSqFt: number;
    materialRatePerSqFt: number;
  }[],
  taxRate: number,
) => {
  let labor = 0,
    material = 0;
  for (const r of rooms) {
    const b = billableSquareFeet(r.lengthFeet, r.widthFeet, r.wastePercentage);
    labor += b * r.laborRatePerSqFt;
    material += b * r.materialRatePerSqFt;
  }
  const subtotal = labor + material;
  const tax = subtotal * (taxRate / 100)
  return { laborSubtotal: labor, materialSubtotal: material, subtotal, taxRate, tax, total: subtotal + tax };
};

export const fmt = (n: number) => n.toLocaleString(undefined, { style: "currency", currency: "USD" });
