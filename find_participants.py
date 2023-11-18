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
# ipv4 neighbor declaration regex
NEIGHBOR_DECL = re.compile(r"^neighbor ([0-9a-f\.:]*)")
IMPORT_POLICIES = re.compile(r"^import \[ (([\w-]* ?)*) \];")


def get_neighbor_import_policies(text: str) -> dict[str, set[str]]:
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
            current_neighbor = neighbor_match.group(1)
            # continue to the next line
            continue
        # now see if the line identifies import policies
        import_match = IMPORT_POLICIES.match(line)
        if current_neighbor is not None and import_match is not None:
            # get all the policies of the import
            policies = set(import_match.group(1).split())
            if current_neighbor not in neighbors_to_policies:
                neighbors_to_policies[current_neighbor] = set()
            neighbors_to_policies[current_neighbor].update(policies)
        else:
            # neither a neighbor nor an import: skip
            continue
    return neighbors_to_policies


if __name__ == "__main__":
    configs = sys.argv[1]
    neighbor_participants = {}
    for config_file in os.listdir(configs):
        with open(os.path.join(configs, config_file), "r") as config:
            text = config.read()
        # get all the PARTICIPANT policies
        policies: list[str] = HAS_PARTICIPANT_TERM.findall(text)
        # for each found policy, find the neighbors that use it
        neighbors_to_policies = get_neighbor_import_policies(text)
        # update the outer collection -- duplicates will be overwritten
        neighbor_participants.update(
            {
                ipaddress.IPv4Address(neighbor): policy.replace("-IN", "-PARTICIPANT")
                # the format of a dict entry
                for policy in policies
                for neighbor, neighbor_policies in neighbors_to_policies.items()
                if policy in neighbor_policies
            }
        )
    output_lines = [
        f'{{"{neighbor}", "{participant}"}},'
        for (neighbor, participant) in sorted(neighbor_participants.items())
    ]
    print("\n".join(output_lines))
