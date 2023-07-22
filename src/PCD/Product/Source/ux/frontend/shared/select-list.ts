//  Item in the select list.
export interface SelectListItem {
    //  Item ID.
    id: string;

    //  Item label, visible to user.
    label: string;
}

export interface Model {
    //  An ID of a selected item. If falsy, no item is selected.
    selectedId?: string;

    //  Items in the list.
    items: SelectListItem[];
}

export function enforceModelConstraints(model: Model): void {
    if (model.selectedId && !_.any(model.items, i => i.id === model.selectedId)) {
        delete model.selectedId;
    }
}
