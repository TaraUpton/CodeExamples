import { createController, prepActions } from "~/hocs/crud";
import { service } from "./service";

export const actions = prepActions(service);
export const productAssignmentSagas = createController(service, actions);