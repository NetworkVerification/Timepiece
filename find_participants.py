#!/usr/bin/env python3
# Helper file to extract all of the participant policies of neighbors
# in a given directory of Juniper configs.
# Essentially, this is a quick-and-dirty Juniper parser for the sole job
# of finding (a) which policy statements refer to prefix lists over participants
# and (b) which neighbors use those policies.
# This all assumes that your configs are written such that their policy statements
# contain an initial "participant" term.
import ipaddress
import os
import re
import sys

# multiline regex to find policies with participant terms
HAS_PARTICIPANT_TERM = re.compile(
    r"policy-statement (\w*-IN) \{\n\s*term participant", flags=re.MULTILINE
)
# neighbor declaration regex
NEIGHBOR_DECL = re.compile(r"^(?:inactive: )?neighbor ([0-9a-f\.:]*)")
IMPORT_POLICIES = re.compile(r"^import \[ (([\w-]* ?)*) \];")


def get_neighbor_import_policies(text: str) -> dict[ipaddress.IPv4Address | ipaddress.IPv6Address, set[str]]:
  """Return a dictionary mapping neighbors to sets of import policy names."""
    # strip the lines
    stripped = [l.strip() for l in text.splitlines()]
    neighbors_to_policies = {}
    # start with no neighbor
    current_neighbor = None
    for line in stripped:
        # first see if the line identifies a neighbor
        neighbor_match = NEIGHBOR_DECL.match(line)
        if neighbor_match is not None:
            # set the current neighbor to fill in
            current_neighbor = ipaddress.ip_address(neighbor_match.group(1))
            # continue to the next line
            continue
        # now see if the line identifies import policies
        # we want to match only one import policy group after each neighbor
        import_match = IMPORT_POLICIES.match(line)
        if current_neighbor is not None and import_match is not None:
            # get all the policies of the import
            policies = set(import_match.group(1).split())
            if current_neighbor not in neighbors_to_policies:
                neighbors_to_policies[current_neighbor] = set()
            neighbors_to_policies[current_neighbor].update(policies)
            # reset current_neighbor
            current_neighbor = None
        else:
            # neither a neighbor nor an import: skip
            continue
    return neighbors_to_policies


if __name__ == "__main__":
    configs = sys.argv[1]
    neighbor_participants = {}
    for config_file in os.listdir(configs):
        with open(os.path.join(configs, config_file), "r") as config:
          config_text = config.read()
        # get all the PARTICIPANT policies
        participant_policies: list[str] = HAS_PARTICIPANT_TERM.findall(config_text)
        # for each found policy, find the neighbors that use it
        neighbors_to_policies = get_neighbor_import_policies(config_text)
        for neighbor in neighbors_to_policies.keys():
          if neighbor in neighbor_participants:
            print(
              f"// Warning: neighbor {neighbor} has value {neighbor_participants[neighbor]} which will be overwritten.")
        # update the outer collection -- duplicates will be overwritten
        neighbor_participants.update(
            {
              neighbor: policy.replace("-IN", "-PARTICIPANT")
                # the format of a dict entry
              for policy in participant_policies
                for neighbor, neighbor_policies in neighbors_to_policies.items()
                if policy in neighbor_policies
            }
        )
    output_lines = [
        f'{{"{neighbor}", "{participant}"}},'
        for (neighbor, participant) in sorted(neighbor_participants.items())
    ]
    print("\n".join(output_lines))
