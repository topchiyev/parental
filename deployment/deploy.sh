#/bin/bash

SECONDS=0

DEP_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
SRC_DIR="$DEP_DIR/.."

pushd "$SRC_DIR"

pushd "backend"
docker buildx build -f Dockerfile --platform=linux/amd64 -t smartscoringcr.azurecr.io/parental-backend:latest --push . || exit 1
popd
pushd "frontend"
docker buildx build -f Dockerfile --platform=linux/amd64 -t smartscoringcr.azurecr.io/parental-frontend:latest --push . || exit 1
popd

popd

pushd "$DEP_DIR"

for i in *.yaml; do
    [ -f "$i" ] || break
    kubectl apply -f $i || exit 1
done

kubectl delete pod --namespace parental -l app=parental -l side=backend || exit 1
kubectl delete pod --namespace parental -l app=parental -l side=frontend || exit 1

popd

ELAPSED="Elapsed: $(($SECONDS / 3600))hrs $((($SECONDS / 60) % 60))min $(($SECONDS % 60))sec"

echo -e "\n$ELAPSED\n"

exit 0
