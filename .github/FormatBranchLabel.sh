# Get the Linux and Windows branch name from the GitHub action's input
BRANCH_NAME_LINUX=$GITHUB_REF_NAME
BRANCH_NAME_WINDOWS=$env:GITHUB_REF_NAME

# Use the Windows branch name if the linux branch name is unset
BRANCH_NAME=${BRANCH_NAME_LINUX:-BRANCH_NAME_WINDOWS}

# Remove '.x' from the end of the branch name.
BRANCH_NAME=${BRANCH_NAME%.x}

# Set the output 'branch-label' to the branch name.
echo "branch-label=${BRANCH_NAME}" >> $GITHUB_OUTPUT
