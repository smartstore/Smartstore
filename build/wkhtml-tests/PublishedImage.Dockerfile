# Extend the exact production candidate; do not rebuild its base or provision wkhtml again.
ARG IMAGE=smartstore-linux:test
FROM ${IMAGE}
WORKDIR /work
RUN apt-get update && apt-get install -y --no-install-recommends python3 poppler-utils && apt-get clean
COPY probe/ /work/.temp/probe/
COPY tests/ /tests/
ENV WKHTML_PROBE_MODE=system
ENTRYPOINT ["python3", "/tests/verify.py"]
