
**Objective:**

Create the first **REST API module** of an ERP application responsible for managing **Truck** data. This API will serve as a foundation for future modules that will manage other resources such as employees, factories, and customers.

**Requirements:**

The API must support full **CRUD operations** for the Truck resource and expose endpoints that allow querying a **filtered and sorted list of trucks**.

Each truck:

- **Must** have a **unique alphanumeric code** provided by the user
- **Must** have a **name**
- **Must** have a **status** selected from the following set:
    - "Out Of Service"
    - "Loading"
    - "To Job"
    - "At Job"
    - "Returning"
- **May** have an optional **description**

**Status transition rules:**

- "Out Of Service" can be set regardless of the current status
- Any status can be set if the current status is "Out Of Service"
- The following statuses must follow this strict order:
    - Loading → To Job → At Job → Returning
- After reaching "Returning", the truck can transition back to "Loading"

**Scope:**

The API should expose endpoints to:

- Create new trucks
- Retrieve single trucks and lists of trucks
- Update truck data, including status changes with validation of transition rules
- Delete trucks
- Query truck lists with filtering and sorting based on selected fields

The solution should be designed with extensibility in mind to support additional ERP modules in the future.

**Hint:**

The code that Claude Code generates by default based on the task description above is solid, but it is not the highest-scoring solution.o