import { useState } from "react";

const today = new Date().toISOString().split("T")[0];

function LookupForms({
  seedData,
  athletes,
  meets,
  events,
  form,
  onChange,
  onCreateAthlete,
  onCreateMeet,
  onCreateEvent,
}) {
  const [newAthlete, setNewAthlete] = useState({
    firstName: "",
    lastName: "",
    gender: "Male",
    dateOfBirth: "",
  });
  const [newMeet, setNewMeet] = useState({
    name: "",
    location: "",
    meetDate: today,
  });
  const [newEvent, setNewEvent] = useState({
    name: "",
    eventType: "Track",
  });

  async function submitNewAthlete(e) {
    e.preventDefault();
    if (!onCreateAthlete) return;
    await onCreateAthlete({
      firstName: newAthlete.firstName.trim(),
      lastName: newAthlete.lastName.trim(),
      gender: newAthlete.gender,
      dateOfBirth: newAthlete.dateOfBirth ? new Date(newAthlete.dateOfBirth).toISOString() : null,
    });
    setNewAthlete({ firstName: "", lastName: "", gender: "Male", dateOfBirth: "" });
  }

  async function submitNewMeet(e) {
    e.preventDefault();
    if (!onCreateMeet) return;
    await onCreateMeet({
      name: newMeet.name.trim(),
      location: newMeet.location.trim(),
      meetDate: new Date(newMeet.meetDate).toISOString(),
    });
  }

  async function submitNewEvent(e) {
    e.preventDefault();
    if (!onCreateEvent) return;
    await onCreateEvent({
      name: newEvent.name.trim(),
      eventType: newEvent.eventType,
    });
    setNewEvent({ name: "", eventType: "Track" });
  }

  return (
    <section className="card">
      <button onClick={seedData}>
        Seed Test Data
      </button>

      <div className="row-wrap">
        <label>
          Athlete
          <select name="athleteId" value={form.athleteId} onChange={onChange} required>
            <option value="" disabled>
              Select athlete
            </option>
            {athletes.map((a) => (
              <option key={a.id} value={a.id}>
                {a.name}
              </option>
            ))}
          </select>
        </label>
        <details className="inline-details">
          <summary>Add new athlete</summary>
          <form onSubmit={submitNewAthlete} className="row-wrap">
            <input
              placeholder="First name"
              value={newAthlete.firstName}
              onChange={(e) => setNewAthlete((prev) => ({ ...prev, firstName: e.target.value }))}
              required
            />
            <input
              placeholder="Last name"
              value={newAthlete.lastName}
              onChange={(e) => setNewAthlete((prev) => ({ ...prev, lastName: e.target.value }))}
              required
            />
            <select
              value={newAthlete.gender}
              onChange={(e) => setNewAthlete((prev) => ({ ...prev, gender: e.target.value }))}
            >
              <option value="Male">Male</option>
              <option value="Female">Female</option>
            </select>
            <input
              type="date"
              value={newAthlete.dateOfBirth}
              onChange={(e) => setNewAthlete((prev) => ({ ...prev, dateOfBirth: e.target.value }))}
            />
            <button type="submit">Add athlete</button>
          </form>
        </details>

        <label>
          Meet
          <select name="meetId" value={form.meetId} onChange={onChange} required>
            <option value="" disabled>
              Select meet
            </option>
            {meets.map((m) => (
              <option key={m.id} value={m.id}>
                {m.name} ({new Date(m.meetDate).toLocaleDateString()})
              </option>
            ))}
          </select>
        </label>
        <details className="inline-details">
          <summary>Add new meet</summary>
          <form onSubmit={submitNewMeet} className="row-wrap">
            <input
              placeholder="Meet name"
              value={newMeet.name}
              onChange={(e) => setNewMeet((prev) => ({ ...prev, name: e.target.value }))}
              required
            />
            <input
              placeholder="Location"
              value={newMeet.location}
              onChange={(e) => setNewMeet((prev) => ({ ...prev, location: e.target.value }))}
              required
            />
            <input
              type="date"
              value={newMeet.meetDate}
              onChange={(e) => setNewMeet((prev) => ({ ...prev, meetDate: e.target.value }))}
              required
            />
            <button type="submit">Add meet</button>
          </form>
        </details>

        <label>
          Event
          <select name="eventId" value={form.eventId} onChange={onChange} required>
            <option value="" disabled>
              Select event
            </option>
            {events.map((ev) => (
              <option key={ev.id} value={ev.id}>
                {ev.name}
              </option>
            ))}
          </select>
        </label>
        <details className="inline-details">
          <summary>Add new event</summary>
          <form onSubmit={submitNewEvent} className="row-wrap">
            <input
              placeholder="Event name"
              value={newEvent.name}
              onChange={(e) => setNewEvent((prev) => ({ ...prev, name: e.target.value }))}
              required
            />
            <select
              value={newEvent.eventType}
              onChange={(e) => setNewEvent((prev) => ({ ...prev, eventType: e.target.value }))}
            >
              <option value="Track">Track</option>
              <option value="Field">Field</option>
            </select>
            <button type="submit">Add event</button>
          </form>
        </details>
      </div>
    </section>
  );
}

export default LookupForms;
